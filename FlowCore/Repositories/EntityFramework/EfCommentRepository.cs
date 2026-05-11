using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfCommentRepository : ICommentRepository
{
    private readonly FlowCoreDbContext _db;

    public EfCommentRepository(FlowCoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<Comment>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Comments
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Comment>> GetByTaskItemIdAsync(Guid taskItemId, CancellationToken ct = default)
    {
        return await _db.Comments
            .AsNoTracking()
            .Where(c => c.TaskItemId == taskItemId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Comments
            .AsNoTracking()
            .Include(c => c.TaskItem)
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Comment> AddAsync(Comment comment, CancellationToken ct = default)
    {
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);
        return comment;
    }

    public async Task<Comment?> UpdateBodyAsync(Guid id, string body, CancellationToken ct = default)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null) return null;
        comment.Body = body;
        comment.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return comment;
    }

    public async Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null)
            return false;

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
