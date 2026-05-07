using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfBoardRepository : IBoardRepository
{
    private readonly FlowCoreDbContext _db;

    public EfBoardRepository(FlowCoreDbContext db) => _db = db;

    public Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default)
    {
        return AsReadOnly(_db.Boards
            .AsNoTracking()
            .Include(b => b.Tasks)
            .OrderBy(b => b.Position)
            .ThenBy(b => b.Name), ct);
    }

    public Task<IReadOnlyList<Board>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return AsReadOnly(_db.Boards
            .AsNoTracking()
            .Where(b => b.ProjectId == projectId)
            .Include(b => b.Tasks)
            .OrderBy(b => b.Position)
            .ThenBy(b => b.Name), ct);
    }

    public Task<Board?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Boards
            .AsNoTracking()
            .AsSplitQuery()
            .Include(b => b.Project)
            .ThenInclude(p => p!.Workspace)
            .Include(b => b.Tasks)
            .ThenInclude(t => t.TaskStatusDefinition)
            .Include(b => b.Tasks)
            .ThenInclude(t => t.Subtasks)
            .Include(b => b.Tasks)
            .ThenInclude(t => t.TaskAssignments)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    private static async Task<IReadOnlyList<T>> AsReadOnly<T>(IQueryable<T> q, CancellationToken ct)
    {
        return await q.ToListAsync(ct);
    }
}
