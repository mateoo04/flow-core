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
        var comments = _db.Comments.AsNoTracking().Include(c => c.Author).AsQueryable();

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

        return Ok(comment.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create([FromBody] CommentCreateDto model, CancellationToken ct)
    {
        if (!await _db.TaskItems.AnyAsync(t => t.Id == model.TaskItemId, ct))
            return BadRequest(new { message = "Task does not exist." });

        if (!await _db.Users.AnyAsync(u => u.Id == model.AuthorUserId, ct))
            return BadRequest(new { message = "Author does not exist." });

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskItemId = model.TaskItemId,
            AuthorUserId = model.AuthorUserId,
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

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
