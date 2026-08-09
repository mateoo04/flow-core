using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using FlowCore.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

[ApiController]
[Route("api/projects")]
public class ProjectsApiController : ControllerBase
{
    private readonly FlowCoreDbContext _db;
    private readonly IValidator<ProjectCreateDto> _createValidator;
    private readonly IValidator<ProjectUpdateDto> _updateValidator;

    public ProjectsApiController(
        FlowCoreDbContext db,
        IValidator<ProjectCreateDto> createValidator,
        IValidator<ProjectUpdateDto> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll(
        [FromQuery] string? query,
        [FromQuery] Guid? workspaceId,
        CancellationToken ct)
    {
        var projects = _db.Projects.AsNoTracking().Include(p => p.Workspace).AsQueryable();

        if (workspaceId is { } wid)
            projects = projects.Where(p => p.WorkspaceId == wid);

        if (!string.IsNullOrWhiteSpace(query))
            projects = projects.Where(p => p.Name.Contains(query) || p.Description.Contains(query));

        var result = (await projects.OrderBy(p => p.Name).ToListAsync(ct))
            .Select(p => p.ToDto())
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken ct)
    {
        var project = await _db.Projects.AsNoTracking()
            .Include(p => p.Workspace)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
            return NotFound();

        return Ok(project.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] ProjectCreateDto model, CancellationToken ct)
    {
        if (!await this.ValidateAndAddToModelStateAsync(_createValidator, model, ct))
            return ValidationProblem(ModelState);

        var workspace = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == model.WorkspaceId, ct);
        if (workspace is null)
            return BadRequest(new { message = "Workspace does not exist." });

        var project = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = model.WorkspaceId,
            Workspace = workspace,
            Name = model.Name,
            Description = model.Description,
            Status = model.Status,
            Priority = model.Priority,
            StartDate = model.StartDate ?? DateTime.UtcNow,
            DueDate = model.DueDate
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, [FromBody] ProjectUpdateDto model, CancellationToken ct)
    {
        if (!await this.ValidateAndAddToModelStateAsync(_updateValidator, model, ct))
            return ValidationProblem(ModelState);

        var project = await _db.Projects.Include(p => p.Workspace).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
            return NotFound();

        project.Name = model.Name;
        project.Description = model.Description;
        project.Status = model.Status;
        project.Priority = model.Priority;
        if (model.StartDate is { } start)
            project.StartDate = start;
        project.DueDate = model.DueDate;
        await _db.SaveChangesAsync(ct);

        return Ok(project.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
            return NotFound();

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
