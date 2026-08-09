using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using FlowCore.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

[ApiController]
[Route("api/tasks")]
public class TasksApiController : ControllerBase
{
    private readonly FlowCoreDbContext _db;
    private readonly IValidator<TaskCreateDto> _createValidator;
    private readonly IValidator<TaskUpdateDto> _updateValidator;

    public TasksApiController(
        FlowCoreDbContext db,
        IValidator<TaskCreateDto> createValidator,
        IValidator<TaskUpdateDto> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private IQueryable<TaskItem> WithRelations(IQueryable<TaskItem> source) =>
        source
            .Include(t => t.TaskStatusDefinition)
            .Include(t => t.TaskAssignments).ThenInclude(a => a.User)
            .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAll(
        [FromQuery] string? query,
        [FromQuery] Guid? boardId,
        CancellationToken ct)
    {
        var tasks = WithRelations(_db.TaskItems.AsNoTracking());

        if (boardId is { } bid)
            tasks = tasks.Where(t => t.BoardId == bid);

        if (!string.IsNullOrWhiteSpace(query))
            tasks = tasks.Where(t => t.Title.Contains(query) || t.Description.Contains(query));

        var result = (await tasks.OrderBy(t => t.Position).ToListAsync(ct))
            .Select(t => t.ToDto())
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> GetById(Guid id, CancellationToken ct)
    {
        var task = await WithRelations(_db.TaskItems.AsNoTracking()).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
            return NotFound();

        return Ok(task.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> Create([FromBody] TaskCreateDto model, CancellationToken ct)
    {
        if (!await this.ValidateAndAddToModelStateAsync(_createValidator, model, ct))
            return ValidationProblem(ModelState);

        if (!await _db.Boards.AnyAsync(b => b.Id == model.BoardId, ct))
            return BadRequest(new { message = "Board does not exist." });

        if (!await _db.TaskStatusDefinitions.AnyAsync(s => s.Id == model.TaskStatusDefinitionId, ct))
            return BadRequest(new { message = "Status does not exist." });

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            BoardId = model.BoardId,
            TaskStatusDefinitionId = model.TaskStatusDefinitionId,
            Title = model.Title,
            Description = model.Description ?? string.Empty,
            Priority = model.Priority,
            StoryPoints = model.StoryPoints,
            ParentTaskItemId = model.ParentTaskItemId,
            DueDate = model.DueDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        AssignUsers(task, model.AssigneeIds, now);
        LinkTags(task, model.TagIds, now);

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync(ct);

        var dto = (await WithRelations(_db.TaskItems.AsNoTracking()).FirstAsync(t => t.Id == task.Id, ct)).ToDto();
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> Update(Guid id, [FromBody] TaskUpdateDto model, CancellationToken ct)
    {
        if (!await this.ValidateAndAddToModelStateAsync(_updateValidator, model, ct))
            return ValidationProblem(ModelState);

        var task = await _db.TaskItems
            .Include(t => t.TaskAssignments)
            .Include(t => t.TaskTags)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
            return NotFound();

        if (!await _db.TaskStatusDefinitions.AnyAsync(s => s.Id == model.TaskStatusDefinitionId, ct))
            return BadRequest(new { message = "Status does not exist." });

        var now = DateTime.UtcNow;
        task.TaskStatusDefinitionId = model.TaskStatusDefinitionId;
        task.Title = model.Title;
        task.Description = model.Description ?? string.Empty;
        task.Priority = model.Priority;
        task.StoryPoints = model.StoryPoints;
        task.DueDate = model.DueDate;
        task.UpdatedAt = now;

        task.TaskAssignments.Clear();
        AssignUsers(task, model.AssigneeIds, now);
        task.TaskTags.Clear();
        LinkTags(task, model.TagIds, now);

        await _db.SaveChangesAsync(ct);

        var dto = (await WithRelations(_db.TaskItems.AsNoTracking()).FirstAsync(t => t.Id == task.Id, ct)).ToDto();
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var task = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
            return NotFound();

        _db.TaskItems.Remove(task);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static void AssignUsers(TaskItem task, IEnumerable<Guid> userIds, DateTime at)
    {
        foreach (var userId in userIds.Distinct())
            task.TaskAssignments.Add(new TaskAssignment { TaskItemId = task.Id, UserId = userId, AssignedAt = at });
    }

    private static void LinkTags(TaskItem task, IEnumerable<Guid> tagIds, DateTime at)
    {
        foreach (var tagId in tagIds.Distinct())
            task.TaskTags.Add(new TaskTag { TaskItemId = task.Id, TagId = tagId, LinkedAt = at });
    }
}
