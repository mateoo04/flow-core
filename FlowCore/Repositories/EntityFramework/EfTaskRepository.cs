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

    public async Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.TaskItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.TaskStatusDefinition)
            .Include(t => t.Board)
            .ThenInclude(b => b!.Project)
            .Include(t => t.TaskAssignments)
            .ThenInclude(a => a.User)
            .Where(t => t.TaskAssignments.Any(a => a.UserId == userId && a.Role == TaskRole.Assignee))
            .ToListAsync(ct);
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
}
