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
        var boards = _db.Boards.AsNoTracking();

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

        return Ok(board.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<BoardDto>> Create([FromBody] BoardCreateDto model, CancellationToken ct)
    {
        var projectExists = await _db.Projects.AnyAsync(p => p.Id == model.ProjectId, ct);
        if (!projectExists)
            return BadRequest(new { message = "Project does not exist." });

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

        _db.Boards.Remove(board);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
