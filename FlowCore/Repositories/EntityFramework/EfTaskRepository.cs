using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfTaskRepository : ITaskRepository
{
    private readonly FlowCoreDbContext _db;

    public EfTaskRepository(FlowCoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.TaskItems
            .AsNoTracking()
            .Include(t => t.Board)
            .OrderBy(t => t.Title)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByBoardIdAsync(Guid boardId, CancellationToken ct = default)
    {
        return await _db.TaskItems
            .AsNoTracking()
            .Where(t => t.BoardId == boardId)
            .Include(t => t.TaskStatusDefinition)
            .OrderBy(t => t.Title)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TaskItem>> SearchAsync(string query, int take, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<TaskItem>();
        var pattern = $"%{query}%";
        return await _db.TaskItems
            .AsNoTracking()
            .Include(t => t.Board)
                .ThenInclude(b => b!.Project)
            .Include(t => t.TaskStatusDefinition)
            .Where(t => EF.Functions.ILike(t.Title, pattern))
            .OrderBy(t => t.Title)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(Guid userId, CancellationToken ct = default)
    {
        var tasks = await _db.TaskItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.TaskStatusDefinition)
            .Include(t => t.Board)
                .ThenInclude(b => b!.Project)
            .Include(t => t.TaskAssignments)
                .ThenInclude(a => a.User)
            .Where(t => t.TaskAssignments.Any(a => a.UserId == userId))
            .ToListAsync(ct);

        var userOrderByTaskId = await _db.UserTaskOrders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .ToDictionaryAsync(o => o.TaskItemId, o => o.Position, ct);

        return tasks
            .OrderBy(t => userOrderByTaskId.ContainsKey(t.Id) ? 0 : 1)
            .ThenBy(t => userOrderByTaskId.TryGetValue(t.Id, out var p) ? p : 0)
            .ThenBy(t => t.Title)
            .ToList();
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.TaskItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Board)
            .ThenInclude(b => b!.Project)
            .Include(t => t.TaskStatusDefinition)
            .Include(t => t.Subtasks)
            .Include(t => t.Comments)
            .ThenInclude(c => c.Author)
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.Tag)
            .Include(t => t.TaskAssignments)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public Task<TaskItem?> GetForEditAsync(Guid id, CancellationToken ct = default)
    {
        return _db.TaskItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Board)
                .ThenInclude(b => b!.Project)
                    .ThenInclude(p => p!.Workspace)
                        .ThenInclude(w => w!.TaskStatusDefinitions)
            .Include(t => t.TaskAssignments)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct = default)
    {
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
            return false;

        _db.TaskItems.Remove(task);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<MoveResult> MoveAsync(
        Guid taskId,
        Guid destinationStatusId,
        int position,
        CancellationToken ct = default)
    {
        if (position < 0) position = 0;

        var strategy = _db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var task = await _db.TaskItems
                .Include(t => t.Board)
                .ThenInclude(b => b!.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task is null) return MoveResult.TaskNotFound;

            var destStatus = await _db.TaskStatusDefinitions
                .FirstOrDefaultAsync(s => s.Id == destinationStatusId, ct);
            if (destStatus is null) return MoveResult.StatusNotFound;

            var taskWorkspaceId = task.Board?.Project?.WorkspaceId;
            if (taskWorkspaceId is null || destStatus.WorkspaceId != taskWorkspaceId)
                return MoveResult.StatusInDifferentWorkspace;

            var sourceStatusId = task.TaskStatusDefinitionId;
            var crossColumn = sourceStatusId != destinationStatusId;

            var destSiblings = await _db.TaskItems
                .Where(t => t.TaskStatusDefinitionId == destinationStatusId && t.Id != taskId)
                .OrderBy(t => t.Position)
                .ToListAsync(ct);

            var clampedPosition = Math.Min(position, destSiblings.Count);

            if (crossColumn)
            {
                var sourceSiblings = await _db.TaskItems
                    .Where(t => t.TaskStatusDefinitionId == sourceStatusId && t.Id != taskId)
                    .OrderBy(t => t.Position)
                    .ToListAsync(ct);

                for (var i = 0; i < sourceSiblings.Count; i++)
                    sourceSiblings[i].Position = i;

                task.TaskStatusDefinitionId = destinationStatusId;
            }

            destSiblings.Insert(clampedPosition, task);
            for (var i = 0; i < destSiblings.Count; i++)
                destSiblings[i].Position = i;

            task.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return MoveResult.Moved;
        });
    }

    public Task<MoveResult> MoveOnHomeAsync(
        Guid currentUserId,
        Guid taskId,
        string destinationStatusName,
        int position,
        CancellationToken ct = default)
    {
        if (position < 0) position = 0;

        var strategy = _db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var task = await _db.TaskItems
                .Include(t => t.Board)
                .ThenInclude(b => b!.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task is null) return MoveResult.TaskNotFound;

            var workspaceId = task.Board?.Project?.WorkspaceId;
            if (workspaceId is null) return MoveResult.StatusNotFound;

            var destStatus = await _db.TaskStatusDefinitions
                .FirstOrDefaultAsync(s =>
                    s.WorkspaceId == workspaceId.Value &&
                    s.Name == destinationStatusName, ct);
            if (destStatus is null) return MoveResult.StatusNotFound;

            if (task.TaskStatusDefinitionId != destStatus.Id)
            {
                var sourceStatusId = task.TaskStatusDefinitionId;

                var sourceSiblings = await _db.TaskItems
                    .Where(t => t.TaskStatusDefinitionId == sourceStatusId && t.Id != taskId)
                    .OrderBy(t => t.Position)
                    .ToListAsync(ct);
                for (var i = 0; i < sourceSiblings.Count; i++)
                    sourceSiblings[i].Position = i;

                var destSiblings = await _db.TaskItems
                    .Where(t => t.TaskStatusDefinitionId == destStatus.Id && t.Id != taskId)
                    .OrderBy(t => t.Position)
                    .ToListAsync(ct);
                destSiblings.Add(task);
                for (var i = 0; i < destSiblings.Count; i++)
                    destSiblings[i].Position = i;

                task.TaskStatusDefinitionId = destStatus.Id;
                task.UpdatedAt = DateTime.UtcNow;
            }

            var siblings = await _db.TaskItems
                .Where(t =>
                    t.TaskAssignments.Any(a => a.UserId == currentUserId) &&
                    t.TaskStatusDefinition!.Name == destinationStatusName &&
                    t.Id != taskId)
                .Select(t => new { t.Id, t.Title })
                .ToListAsync(ct);

            var siblingIds = siblings.Select(s => s.Id).ToList();

            var existingOrders = await _db.UserTaskOrders
                .Where(o => o.UserId == currentUserId && siblingIds.Contains(o.TaskItemId))
                .ToDictionaryAsync(o => o.TaskItemId, o => o.Position, ct);

            var orderedSiblingIds = siblings
                .OrderBy(s => existingOrders.ContainsKey(s.Id) ? 0 : 1)
                .ThenBy(s => existingOrders.TryGetValue(s.Id, out var p) ? p : 0)
                .ThenBy(s => s.Title)
                .Select(s => s.Id)
                .ToList();

            var clampedPosition = Math.Min(position, orderedSiblingIds.Count);
            orderedSiblingIds.Insert(clampedPosition, taskId);

            for (var i = 0; i < orderedSiblingIds.Count; i++)
            {
                var tid = orderedSiblingIds[i];
                var existing = await _db.UserTaskOrders
                    .FirstOrDefaultAsync(o => o.UserId == currentUserId && o.TaskItemId == tid, ct);
                if (existing is null)
                {
                    _db.UserTaskOrders.Add(new UserTaskOrder
                    {
                        UserId = currentUserId,
                        TaskItemId = tid,
                        Position = i
                    });
                }
                else
                {
                    existing.Position = i;
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return MoveResult.Moved;
        });
    }
}
