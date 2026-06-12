using System.Security.Claims;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

[ApiController]
[Route("api/comments")]
public class CommentsApiController : ControllerBase
{
    private readonly FlowCoreDbContext _db;

    public CommentsApiController(FlowCoreDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetAll(
        [FromQuery] Guid? taskItemId,
        CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        var comments = _db.Comments
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
        var comment = await _db.Comments.AsNoTracking()
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
        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        if (!await _db.TaskItems.AnyAsync(t => t.Id == model.TaskItemId, ct))
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

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var dto = (await _db.Comments.AsNoTracking()
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id, ct)).ToDto();
        return CreatedAtAction(nameof(GetById), new { id = comment.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CommentDto>> Update(Guid id, [FromBody] CommentUpdateDto model, CancellationToken ct)
    {
        var comment = await _db.Comments.Include(c => c.Author).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null)
            return NotFound();
        if (!CanModifyComment(comment) || !await CanAccessCommentAsync(comment.Id, ct))
            return Forbid();

        comment.Body = model.Body;
        comment.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(comment.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null)
            return NotFound();
        if (!CanModifyComment(comment) || !await CanAccessCommentAsync(comment.Id, ct))
            return Forbid();

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private bool CanModifyComment(Comment comment) =>
        User.IsInRole(AppRoles.Admin) || comment.AuthorUserId == CurrentUserId();

    private async Task<bool> CanAccessCommentAsync(Guid commentId, CancellationToken ct)
    {
        var taskId = await _db.Comments
            .Where(c => c.Id == commentId)
            .Select(c => c.TaskItemId)
            .FirstOrDefaultAsync(ct);

        return taskId != Guid.Empty && await CanAccessTaskAsync(taskId, ct);
    }

    private async Task<bool> CanAccessTaskAsync(Guid taskId, CancellationToken ct)
    {
        if (User.IsInRole(AppRoles.Admin))
            return true;

        if (CurrentUserId() is not { } userId)
            return false;

        return await _db.TaskItems.AnyAsync(t =>
            t.Id == taskId &&
            t.Board != null &&
            t.Board.Project != null &&
            t.Board.Project.Workspace!.Members.Any(m => m.UserId == userId), ct);
    }
}
