using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfTagRepository : ITagRepository
{
    private readonly FlowCoreDbContext _db;

    public EfTagRepository(FlowCoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public Task<Tag?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Tags
            .AsNoTracking()
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.TaskItem)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tag> AddAsync(Tag tag, CancellationToken ct = default)
    {
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync(ct);
        return tag;
    }

    public async Task<Tag?> UpdateAsync(Guid id, string name, string colorHex, CancellationToken ct = default)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null) return null;
        tag.Name = name;
        tag.ColorHex = colorHex;
        await _db.SaveChangesAsync(ct);
        return tag;
    }

    public async Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null) return false;
        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        return _db.Tags.AnyAsync(t =>
            t.Name == trimmed && (excludeId == null || t.Id != excludeId), ct);
    }
}
