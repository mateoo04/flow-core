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
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
