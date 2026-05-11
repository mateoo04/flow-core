using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfProjectRepository : IProjectRepository
{
    private readonly FlowCoreDbContext _db;

    public EfProjectRepository(FlowCoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Projects
            .AsNoTracking()
            .Include(p => p.Boards)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Project>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _db.Projects
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId)
            .Include(p => p.Boards)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Project>> SearchAsync(string query, int take, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<Project>();
        var pattern = $"%{query}%";
        return await _db.Projects
            .AsNoTracking()
            .Include(p => p.Workspace)
            .Where(p => EF.Functions.ILike(p.Name, pattern))
            .OrderBy(p => p.Name)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Projects
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(p => p.Workspace)
            .ThenInclude(w => w!.TaskStatusDefinitions)
            .Include(p => p.Boards)
            .ThenInclude(b => b.Tasks)
            .ThenInclude(t => t.TaskStatusDefinition)
            .Include(p => p.Boards)
            .ThenInclude(b => b.Tasks)
            .ThenInclude(t => t.Subtasks)
            .Include(p => p.Boards)
            .ThenInclude(b => b.Tasks)
            .ThenInclude(t => t.TaskAssignments)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Project> AddAsync(Project project, CancellationToken ct = default)
    {
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);
        return project;
    }

    public async Task<Project?> UpdateAsync(
        Guid id,
        string name,
        string description,
        ProjectStatus status,
        ProjectPriority priority,
        DateTime? startDate,
        DateTime? dueDate,
        CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return null;

        project.Name = name;
        project.Description = description;
        project.Status = status;
        project.Priority = priority;
        if (startDate is { } sd) project.StartDate = sd;
        project.DueDate = dueDate;

        await _db.SaveChangesAsync(ct);
        return project;
    }

    public async Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
            return false;

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
