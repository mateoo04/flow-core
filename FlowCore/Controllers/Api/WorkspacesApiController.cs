using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

[ApiController]
[Route("api/workspaces")]
public class WorkspacesApiController : ControllerBase
{
    private readonly FlowCoreDbContext _db;

    public WorkspacesApiController(FlowCoreDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkspaceDto>>> GetAll([FromQuery] string? query, CancellationToken ct)
    {
        var workspaces = _db.Workspaces.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
            workspaces = workspaces.Where(w => w.Name.Contains(query) || w.Description.Contains(query));

        var result = (await workspaces.OrderBy(w => w.Name).ToListAsync(ct))
            .Select(w => w.ToDto())
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkspaceDto>> GetById(Guid id, CancellationToken ct)
    {
        var workspace = await _db.Workspaces.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);
        if (workspace is null)
            return NotFound();

        return Ok(workspace.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> Create([FromBody] WorkspaceCreateDto model, CancellationToken ct)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            Description = model.Description,
            Visibility = model.Visibility,
            CreatedAt = DateTime.UtcNow
        };

        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = workspace.Id }, workspace.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkspaceDto>> Update(Guid id, [FromBody] WorkspaceUpdateDto model, CancellationToken ct)
    {
        var workspace = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (workspace is null)
            return NotFound();

        workspace.Name = model.Name;
        workspace.Description = model.Description;
        workspace.Visibility = model.Visibility;
        await _db.SaveChangesAsync(ct);

        return Ok(workspace.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var workspace = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (workspace is null)
            return NotFound();

        _db.Workspaces.Remove(workspace);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
