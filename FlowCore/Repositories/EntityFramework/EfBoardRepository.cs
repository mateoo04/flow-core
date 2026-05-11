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
            .AsNoTrackingWithIdentityResolution()
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

    public async Task<Board> AddAsync(Guid projectId, string name, bool isDefault, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var maxPos = await _db.Boards
            .Where(b => b.ProjectId == projectId)
            .Select(b => (int?)b.Position)
            .MaxAsync(ct) ?? -1;

        if (isDefault)
            await ClearDefaultsAsync(projectId, excludeId: null, ct);

        var board = new Board
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            Position = maxPos + 1,
            IsDefault = isDefault,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Boards.Add(board);
        await _db.SaveChangesAsync(ct);
        return board;
    }

    public async Task<Board?> UpdateAsync(Guid id, string name, bool isDefault, CancellationToken ct = default)
    {
        var board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (board is null) return null;

        if (isDefault && !board.IsDefault)
            await ClearDefaultsAsync(board.ProjectId, excludeId: id, ct);

        board.Name = name;
        board.IsDefault = isDefault;
        board.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return board;
    }

    public async Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (board is null) return false;
        _db.Boards.Remove(board);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> NameExistsInProjectAsync(Guid projectId, string name, Guid? excludeId, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        return _db.Boards.AnyAsync(b =>
            b.ProjectId == projectId
            && b.Name == trimmed
            && (excludeId == null || b.Id != excludeId), ct);
    }

    private async Task ClearDefaultsAsync(Guid projectId, Guid? excludeId, CancellationToken ct)
    {
        var siblings = await _db.Boards
            .Where(b => b.ProjectId == projectId && b.IsDefault && (excludeId == null || b.Id != excludeId))
            .ToListAsync(ct);
        foreach (var s in siblings) s.IsDefault = false;
    }

    private static async Task<IReadOnlyList<T>> AsReadOnly<T>(IQueryable<T> q, CancellationToken ct)
    {
        return await q.ToListAsync(ct);
    }
}
