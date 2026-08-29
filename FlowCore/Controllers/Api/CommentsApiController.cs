using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using FlowCore.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

[ApiController]
[Route("api/comments")]
public class CommentsApiController : WorkspaceApiControllerBase
{
    private readonly IValidator<CommentCreateDto> _createValidator;
    private readonly IValidator<CommentUpdateDto> _updateValidator;

    public CommentsApiController(
        FlowCoreDbContext db,
        IValidator<CommentCreateDto> createValidator,
        IValidator<CommentUpdateDto> updateValidator)
    : base(db)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetAll(
        [FromQuery] Guid? taskItemId,
        CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        var comments = Db.Comments
            .AsNoTracking()
            .Include(c => c.Author)
            .AsQueryable();

        if (!User.IsInRole(AppRoles.Admin))
        {
            comments = comments.Where(c =>
                c.TaskItem != null &&
                c.TaskItem.Board != null &&
                c.TaskItem.Board.Project != null &&
                c.TaskItem.Board.Project.Workspace!.Members.Any(m => m.UserId == userId));
        }

        if (taskItemId is { } tid)
            comments = comments.Where(c => c.TaskItemId == tid);

        var result = (await comments.OrderByDescending(c => c.CreatedAt).ToListAsync(ct))
            .Select(c => c.ToDto())
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CommentDto>> GetById(Guid id, CancellationToken ct)
    {
        var comment = await Db.Comments.AsNoTracking()
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null)
            return NotFound();
        if (!await CanAccessCommentAsync(comment.Id, ct))
            return Forbid();

        return Ok(comment.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create([FromBody] CommentCreateDto model, CancellationToken ct)
    {
        if (!await this.ValidateAndAddToModelStateAsync(_createValidator, model, ct))
            return ValidationProblem(ModelState);

        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        if (!await Db.TaskItems.AnyAsync(t => t.Id == model.TaskItemId, ct))
            return BadRequest(new { message = "Task does not exist." });

        if (!await CanAccessTaskAsync(model.TaskItemId, ct))
            return Forbid();

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskItemId = model.TaskItemId,
            AuthorUserId = userId,
            Body = model.Body,
            CreatedAt = DateTime.UtcNow
        };

        Db.Comments.Add(comment);
        await Db.SaveChangesAsync(ct);

        var dto = (await Db.Comments.AsNoTracking()
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id, ct)).ToDto();
        return CreatedAtAction(nameof(GetById), new { id = comment.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CommentDto>> Update(Guid id, [FromBody] CommentUpdateDto model, CancellationToken ct)
    {
        if (!await this.ValidateAndAddToModelStateAsync(_updateValidator, model, ct))
            return ValidationProblem(ModelState);

        var comment = await Db.Comments.Include(c => c.Author).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null)
            return NotFound();
        if (!CanModifyComment(comment) || !await CanAccessCommentAsync(comment.Id, ct))
            return Forbid();

        comment.Body = model.Body;
        comment.EditedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync(ct);

        return Ok(comment.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var comment = await Db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null)
            return NotFound();
        if (!CanModifyComment(comment) || !await CanAccessCommentAsync(comment.Id, ct))
            return Forbid();

        Db.Comments.Remove(comment);
        await Db.SaveChangesAsync(ct);

        return NoContent();
    }

    private bool CanModifyComment(Comment comment) =>
        User.IsInRole(AppRoles.Admin) || comment.AuthorUserId == CurrentUserId();

    private async Task<bool> CanAccessCommentAsync(Guid commentId, CancellationToken ct)
    {
        var taskId = await Db.Comments
            .Where(c => c.Id == commentId)
            .Select(c => c.TaskItemId)
            .FirstOrDefaultAsync(ct);

        return taskId != Guid.Empty && await CanAccessTaskAsync(taskId, ct);
    }

    private async Task<bool> CanAccessTaskAsync(Guid taskId, CancellationToken ct)
    {
        var workspaceId = await Db.TaskItems
            .Where(task => task.Id == taskId)
            .Select(task => (Guid?)task.Board!.Project!.WorkspaceId)
            .FirstOrDefaultAsync(ct);

        return workspaceId is not null && await CanAccessWorkspaceAsync(workspaceId.Value, ct);
    }
}
