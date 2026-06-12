using System.Security.Claims;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

[ApiController]
[Route("api/boards")]
public class BoardsApiController : ControllerBase
{
    private readonly FlowCoreDbContext _db;

    public BoardsApiController(FlowCoreDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoardDto>>> GetAll(
        [FromQuery] string? query,
        [FromQuery] Guid? projectId,
        CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
            return Unauthorized();

        var boards = _db.Boards.AsNoTracking().AsQueryable();

        if (!User.IsInRole(AppRoles.Admin))
        {
            boards = boards.Where(b =>
                b.Project != null &&
                b.Project.Workspace!.Members.Any(m => m.UserId == userId));
        }

        if (projectId is { } pid)
            boards = boards.Where(b => b.ProjectId == pid);

        if (!string.IsNullOrWhiteSpace(query))
            boards = boards.Where(b => b.Name.Contains(query));

        var result = (await boards.OrderBy(b => b.Position).ToListAsync(ct))
            .Select(b => b.ToDto())
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoardDto>> GetById(Guid id, CancellationToken ct)
    {
        var board = await _db.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        if (board is null)
            return NotFound();
        if (!await CanAccessBoardAsync(id, ct))
            return Forbid();

        return Ok(board.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<BoardDto>> Create([FromBody] BoardCreateDto model, CancellationToken ct)
    {
        var workspaceId = await WorkspaceIdForProjectAsync(model.ProjectId, ct);
        if (workspaceId is null)
            return BadRequest(new { message = "Project does not exist." });
        if (!await CanAccessWorkspaceAsync(workspaceId.Value, ct))
            return Forbid();

        var now = DateTime.UtcNow;
        var board = new Board
        {
            Id = Guid.NewGuid(),
            ProjectId = model.ProjectId,
            Name = model.Name,
            Position = model.Position,
            IsDefault = model.IsDefault,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Boards.Add(board);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = board.Id }, board.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BoardDto>> Update(Guid id, [FromBody] BoardUpdateDto model, CancellationToken ct)
    {
        var board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (board is null)
            return NotFound();
        if (!await CanAccessBoardAsync(id, ct))
            return Forbid();

        board.Name = model.Name;
        board.Position = model.Position;
        board.IsDefault = model.IsDefault;
        board.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(board.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (board is null)
            return NotFound();
        if (!await CanAccessBoardAsync(id, ct))
            return Forbid();

        _db.Boards.Remove(board);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private async Task<Guid?> WorkspaceIdForProjectAsync(Guid projectId, CancellationToken ct) =>
        await _db.Projects
            .Where(p => p.Id == projectId)
            .Select(p => (Guid?)p.WorkspaceId)
            .FirstOrDefaultAsync(ct);

    private async Task<Guid?> WorkspaceIdForBoardAsync(Guid boardId, CancellationToken ct) =>
        await _db.Boards
            .Where(b => b.Id == boardId)
            .Select(b => (Guid?)b.Project!.WorkspaceId)
            .FirstOrDefaultAsync(ct);

    private async Task<bool> CanAccessBoardAsync(Guid boardId, CancellationToken ct)
    {
        var workspaceId = await WorkspaceIdForBoardAsync(boardId, ct);
        return workspaceId is not null && await CanAccessWorkspaceAsync(workspaceId.Value, ct);
    }

    private async Task<bool> CanAccessWorkspaceAsync(Guid workspaceId, CancellationToken ct)
    {
        if (User.IsInRole(AppRoles.Admin))
            return true;

        if (CurrentUserId() is not { } userId)
            return false;

        return await _db.WorkspaceMembers.AnyAsync(m =>
            m.WorkspaceId == workspaceId && m.UserId == userId, ct);
    }
}
