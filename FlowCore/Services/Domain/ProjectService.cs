using FlowCore.Common;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Services.Domain;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;
    private readonly FlowCoreDbContext _db;

    public ProjectService(IProjectRepository projects, FlowCoreDbContext db)
    {
        _projects = projects;
        _db = db;
    }

    public async Task<Result<Project>> CreateInWorkspaceAsync(
        Guid workspaceId,
        string name,
        string description,
        ProjectStatus status,
        ProjectPriority priority,
        DateTime? startDate,
        DateTime? dueDate,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Validation<Project>("Project name is required.");

        var workspace = await _db.Workspaces
            .Include(w => w.TaskStatusDefinitions)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (workspace is null)
            return Result.NotFound<Project>("Workspace not found.");

        var ordered = workspace.TaskStatusDefinitions.OrderBy(s => s.Position).ToList();
        if (ordered.Count < 4)
            return Result.Validation<Project>("Workspace needs at least four task statuses before a project can be created.");

        var statuses = new WorkspaceStatuses
        {
            Backlog = ordered[0],
            Todo = ordered[1],
            InProgress = ordered[2],
            Done = ordered.LastOrDefault(s => s.IsDoneState) ?? ordered[^1]
        };

        var ctx = ProjectBlueprint.CreateProject(
            workspace,
            Guid.NewGuid,
            DateTime.UtcNow,
            statuses,
            name.Trim(),
            description?.Trim() ?? "",
            status,
            priority,
            NormalizeUtc(startDate),
            NormalizeUtc(dueDate));

        return Result.Ok(await _projects.AddAsync(ctx.Project, ct));
    }

    private static DateTime? NormalizeUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } v => v,
        var v => DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
    };
}
