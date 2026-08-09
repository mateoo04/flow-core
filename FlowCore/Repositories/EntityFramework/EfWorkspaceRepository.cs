using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfWorkspaceRepository : IWorkspaceRepository
{
    private readonly FlowCoreDbContext _db;

    public EfWorkspaceRepository(FlowCoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<Workspace>> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Workspaces
            .AsNoTracking()
            .AsSplitQuery()
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .Include(w => w.Projects)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);
    }

    public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Workspaces
            .AsNoTracking()
            .AsSplitQuery()
            .Include(w => w.Projects)
            .Include(w => w.TaskStatusDefinitions)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<Workspace> AddAsync(Workspace workspace, Guid ownerUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var statuses = ProjectBlueprint.CreateWorkspaceStatuses(workspace.Id, now, Guid.NewGuid);
        workspace.TaskStatusDefinitions.Add(statuses.Backlog);
        workspace.TaskStatusDefinitions.Add(statuses.Todo);
        workspace.TaskStatusDefinitions.Add(statuses.InProgress);
        workspace.TaskStatusDefinitions.Add(statuses.Done);

        workspace.Members.Add(new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = ownerUserId,
            Role = WorkspaceRole.Owner,
            JoinedAt = now
        });

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

    public Task<WorkspaceMember?> GetMembershipAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
    {
        return _db.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<WorkspaceMember>> GetMembersAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _db.WorkspaceMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.WorkspaceId == workspaceId)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.User.FullName)
            .ToListAsync(ct);
    }

    public async Task<WorkspaceMember?> AddMemberAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct = default)
    {
        var existing = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
        if (existing is not null) return null;

        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
        _db.WorkspaceMembers.Add(member);
        await _db.SaveChangesAsync(ct);
        return member;
    }

    public Task<bool> RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var member = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
            if (member is null) return false;

            // Cascade: drop the user's TaskAssignments and UserTaskOrders for tasks in this workspace.
            var taskIds = await _db.TaskItems
                .Where(t => t.Board.Project.WorkspaceId == workspaceId)
                .Select(t => t.Id)
                .ToListAsync(ct);

            var assignments = _db.TaskAssignments
                .Where(a => a.UserId == userId && taskIds.Contains(a.TaskItemId));
            _db.TaskAssignments.RemoveRange(assignments);

            var orders = _db.UserTaskOrders
                .Where(o => o.UserId == userId && taskIds.Contains(o.TaskItemId));
            _db.UserTaskOrders.RemoveRange(orders);

            _db.WorkspaceMembers.Remove(member);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        });
    }

    public Task<bool> TransferOwnershipAsync(Guid workspaceId, Guid newOwnerUserId, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var members = await _db.WorkspaceMembers
                .Where(m => m.WorkspaceId == workspaceId)
                .ToListAsync(ct);

            var currentOwner = members.FirstOrDefault(m => m.Role == WorkspaceRole.Owner);
            var newOwner = members.FirstOrDefault(m => m.UserId == newOwnerUserId);
            if (currentOwner is null || newOwner is null) return false;

            if (currentOwner.UserId == newOwnerUserId) return true; // no-op

            currentOwner.Role = WorkspaceRole.Member;
            newOwner.Role = WorkspaceRole.Owner;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        });
    }
}
