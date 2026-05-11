using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfWorkspaceRepository : IWorkspaceRepository
{
    private readonly FlowCoreDbContext _db;

    public EfWorkspaceRepository(FlowCoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Workspaces
            .AsNoTracking()
            .Include(w => w.Projects)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);
    }

    public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Workspaces
            .AsNoTracking()
            .AsSplitQuery()
            .Include(w => w.Owner)
            .Include(w => w.Projects)
            .Include(w => w.TaskStatusDefinitions)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var statuses = ProjectBlueprint.CreateWorkspaceStatuses(workspace.Id, now, Guid.NewGuid);
        workspace.TaskStatusDefinitions.Add(statuses.Backlog);
        workspace.TaskStatusDefinitions.Add(statuses.Todo);
        workspace.TaskStatusDefinitions.Add(statuses.InProgress);
        workspace.TaskStatusDefinitions.Add(statuses.Done);

        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync(ct);
        return workspace;
    }

    public async Task<Workspace?> UpdateAsync(
        Guid id,
        string name,
        string description,
        WorkspaceVisibility visibility,
        CancellationToken ct = default)
    {
        var ws = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (ws is null) return null;

        ws.Name = name;
        ws.Description = description;
        ws.Visibility = visibility;
        await _db.SaveChangesAsync(ct);
        return ws;
    }

    public async Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var ws = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (ws is null) return false;
        _db.Workspaces.Remove(ws);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> HasProjectsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return _db.Projects.AnyAsync(p => p.WorkspaceId == workspaceId, ct);
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        return _db.Workspaces.AnyAsync(w =>
            w.Name == trimmed && (excludeId == null || w.Id != excludeId), ct);
    }
}
