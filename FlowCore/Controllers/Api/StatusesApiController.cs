using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

[ApiController]
[Route("api/statuses")]
public class StatusesApiController : ControllerBase
{
    private readonly FlowCoreDbContext _db;

    public StatusesApiController(FlowCoreDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StatusDto>>> GetAll(
        [FromQuery] string? query,
        [FromQuery] Guid? workspaceId,
        CancellationToken ct)
    {
        var statuses = _db.TaskStatusDefinitions.AsNoTracking();

        if (workspaceId is { } wid)
            statuses = statuses.Where(s => s.WorkspaceId == wid);

        if (!string.IsNullOrWhiteSpace(query))
            statuses = statuses.Where(s => s.Name.Contains(query));

        var result = (await statuses.OrderBy(s => s.Position).ToListAsync(ct))
            .Select(s => s.ToDto())
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StatusDto>> GetById(Guid id, CancellationToken ct)
    {
        var status = await _db.TaskStatusDefinitions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (status is null)
            return NotFound();

        return Ok(status.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<StatusDto>> Create([FromBody] StatusCreateDto model, CancellationToken ct)
    {
        var workspaceExists = await _db.Workspaces.AnyAsync(w => w.Id == model.WorkspaceId, ct);
        if (!workspaceExists)
            return BadRequest(new { message = "Workspace does not exist." });

        var status = new TaskStatusDefinition
        {
            Id = Guid.NewGuid(),
            WorkspaceId = model.WorkspaceId,
            Name = model.Name,
            ColorHex = model.ColorHex,
            Position = model.Position,
            IsDoneState = model.IsDoneState,
            CreatedAt = DateTime.UtcNow
        };

        _db.TaskStatusDefinitions.Add(status);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = status.Id }, status.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StatusDto>> Update(Guid id, [FromBody] StatusUpdateDto model, CancellationToken ct)
    {
        var status = await _db.TaskStatusDefinitions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (status is null)
            return NotFound();

        status.Name = model.Name;
        status.ColorHex = model.ColorHex;
        status.Position = model.Position;
        status.IsDoneState = model.IsDoneState;
        await _db.SaveChangesAsync(ct);

        return Ok(status.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var status = await _db.TaskStatusDefinitions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (status is null)
            return NotFound();

        // TaskItem -> TaskStatusDefinition uses DeleteBehavior.Restrict; block deletion if any task still uses it.
        var inUse = await _db.TaskItems.AnyAsync(t => t.TaskStatusDefinitionId == id, ct);
        if (inUse)
            return Conflict(new { message = "Status is still used by tasks and cannot be deleted." });

        _db.TaskStatusDefinitions.Remove(status);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
