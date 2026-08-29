using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using FlowCore.Repositories;
using FlowCore.Services.Domain;
using FlowCore.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

[ApiController]
[Route("api/tasks")]
public class TasksApiController : WorkspaceApiControllerBase
{
    private readonly IValidator<TaskCreateDto> _createValidator;
    private readonly IValidator<TaskUpdateDto> _updateValidator;
    private readonly ITaskService _taskService;

    public TasksApiController(
        FlowCoreDbContext db,
        IValidator<TaskCreateDto> createValidator,
        IValidator<TaskUpdateDto> updateValidator,
        ITaskService taskService)
    : base(db)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _taskService = taskService;
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
        var tasks = WithRelations(Db.TaskItems.AsNoTracking());

        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        if (!User.IsInRole(AppRoles.Admin))
        {
            tasks = tasks.Where(t => t.Board != null && t.Board.Project != null &&
                t.Board.Project.Workspace!.Members.Any(m => m.UserId == userId));
        }

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
        var task = await WithRelations(Db.TaskItems.AsNoTracking()).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
            return NotFound();
        if (!await CanAccessTaskAsync(id, ct))
            return Forbid();

        return Ok(task.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> Create([FromBody] TaskCreateDto model, CancellationToken ct)
    {
        if (!await this.ValidateAndAddToModelStateAsync(_createValidator, model, ct))
            return ValidationProblem(ModelState);

        var workspaceId = await WorkspaceIdForBoardAsync(model.BoardId, ct);
        if (workspaceId is null)
            return BadRequest(new { message = "Board does not exist." });
        if (!await CanAccessWorkspaceAsync(workspaceId.Value, ct))
            return Forbid();

        var result = await _taskService.CreateAsync(new CreateTaskRequest(
            model.BoardId,
            model.TaskStatusDefinitionId,
            model.Title,
            model.Description,
            model.Priority,
            model.StoryPoints,
            model.ParentTaskItemId,
            model.DueDate,
            model.AssigneeIds,
            model.TagIds), ct);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error!.Value);

        var task = result.Value!;
        var dto = (await WithRelations(Db.TaskItems.AsNoTracking()).FirstAsync(item => item.Id == task.Id, ct)).ToDto();
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskItemDto>> Update(Guid id, [FromBody] TaskUpdateDto model, CancellationToken ct)
    {
        if (!await this.ValidateAndAddToModelStateAsync(_updateValidator, model, ct))
            return ValidationProblem(ModelState);

        var task = await Db.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
            return NotFound();
        if (!await CanAccessTaskAsync(id, ct))
            return Forbid();

        var result = await _taskService.UpdateAsync(new UpdateTaskRequest(
            id,
            model.TaskStatusDefinitionId,
            model.Title,
            model.Description,
            model.Priority,
            model.StoryPoints,
            model.DueDate,
            model.AssigneeIds,
            model.TagIds), ct);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error!.Value);

        var dto = (await WithRelations(Db.TaskItems.AsNoTracking()).FirstAsync(item => item.Id == id, ct)).ToDto();
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var task = await Db.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
            return NotFound();
        if (!await CanAccessTaskAsync(id, ct))
            return Forbid();

        var result = await _taskService.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return ToFailureResult(result.Error!.Value);

        return NoContent();
    }

    private async Task<Guid?> WorkspaceIdForBoardAsync(Guid boardId, CancellationToken ct) =>
        await Db.Boards
            .Where(b => b.Id == boardId)
            .Select(b => (Guid?)b.Project!.WorkspaceId)
            .FirstOrDefaultAsync(ct);

    private async Task<bool> CanAccessTaskAsync(Guid taskId, CancellationToken ct)
    {
        var workspaceId = await Db.TaskItems
            .Where(t => t.Id == taskId)
            .Select(t => (Guid?)t.Board!.Project!.WorkspaceId)
            .FirstOrDefaultAsync(ct);
        return workspaceId is not null && await CanAccessWorkspaceAsync(workspaceId.Value, ct);
    }

    private ActionResult ToFailureResult(Common.ResultError error) => error.Kind switch
    {
        Common.ErrorKind.NotFound => NotFound(new { message = error.Message }),
        Common.ErrorKind.Conflict => Conflict(new { message = error.Message }),
        _ => BadRequest(new { message = error.Message })
    };

}
