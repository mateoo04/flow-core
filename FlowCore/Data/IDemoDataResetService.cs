using FlowCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Data;

public interface IDemoDataResetService
{
    Task ResetAsync(CancellationToken ct = default);
}

public sealed class DemoDataResetService : IDemoDataResetService
{
    private readonly FlowCoreDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public DemoDataResetService(FlowCoreDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var seededUserIds = new[]
        {
            DemoSeedIds.UserAlex,
            DemoSeedIds.UserSam,
            DemoSeedIds.UserCasey,
            DemoSeedIds.UserJordan,
            DemoSeedIds.UserMorgan,
            DemoSeedIds.UserDemo,
        };
        var seededTagIds = new[] { DemoSeedIds.TagUi, DemoSeedIds.TagBug };

        // Workspaces owned by any seeded user — these get wiped + rebuilt.
        var seededWorkspaceIds = await _db.WorkspaceMembers
            .Where(m => seededUserIds.Contains(m.UserId) && m.Role == WorkspaceRole.Owner)
            .Select(m => m.WorkspaceId)
            .Distinct()
            .ToListAsync(ct);

        // Load workspaces with their full graphs so EF tracks every dependent
        // and issues the correct cascading DELETEs in dependency order.
        var workspacesToDelete = await _db.Workspaces
            .Include(w => w.TaskStatusDefinitions)
            .Include(w => w.Projects)
                .ThenInclude(p => p.Boards)
                    .ThenInclude(b => b.Tasks)
                        .ThenInclude(t => t.TaskAssignments)
            .Include(w => w.Projects)
                .ThenInclude(p => p.Boards)
                    .ThenInclude(b => b.Tasks)
                        .ThenInclude(t => t.TaskTags)
            .Include(w => w.Projects)
                .ThenInclude(p => p.Boards)
                    .ThenInclude(b => b.Tasks)
                        .ThenInclude(t => t.Comments)
            .Include(w => w.Members)
            .Where(w => seededWorkspaceIds.Contains(w.Id))
            .ToListAsync(ct);

        _db.Workspaces.RemoveRange(workspacesToDelete);

        // Defensive: drop any leftover memberships of seeded users in workspaces
        // outside the seed.
        var leftoverMemberships = await _db.WorkspaceMembers
            .Where(m => seededUserIds.Contains(m.UserId) && !seededWorkspaceIds.Contains(m.WorkspaceId))
            .ToListAsync(ct);
        _db.WorkspaceMembers.RemoveRange(leftoverMemberships);

        await _db.SaveChangesAsync(ct);

        // Re-build the seed graph. Skip users/tags that already exist (Identity
        // rows are preserved so the visitor's cookie stays valid).
        var graph = DemoDataBuilder.CreateSampleGraph(_hasher);

        var existingUserIds = (await _db.Users
            .Where(u => seededUserIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(ct)).ToHashSet();

        var existingTagIds = (await _db.Tags
            .Where(t => seededTagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(ct)).ToHashSet();

        _db.Users.AddRange(graph.Users.Where(u => !existingUserIds.Contains(u.Id)));
        _db.Tags.AddRange(graph.Tags.Where(t => !existingTagIds.Contains(t.Id)));

        // Null out User/Tag navigations on reachable rows for already-existing
        // users/tags, so EF doesn't try to insert duplicates by walking
        // navigation from a tracked Workspace.
        foreach (var ws in graph.Workspaces)
        foreach (var project in ws.Projects)
        foreach (var board in project.Boards)
        foreach (var task in board.Tasks)
        {
            foreach (var ta in task.TaskAssignments)
                if (existingUserIds.Contains(ta.UserId))
                    ta.User = null!;
            foreach (var tt in task.TaskTags)
                if (existingTagIds.Contains(tt.TagId))
                    tt.Tag = null!;
        }

        foreach (var m in graph.WorkspaceMembers)
            if (existingUserIds.Contains(m.UserId))
                m.User = null!;

        _db.Workspaces.AddRange(graph.Workspaces);
        _db.WorkspaceMembers.AddRange(graph.WorkspaceMembers);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
