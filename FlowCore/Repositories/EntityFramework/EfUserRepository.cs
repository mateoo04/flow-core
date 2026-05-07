using FlowCore.Data;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Repositories.EntityFramework;

public sealed class EfUserRepository : IUserRepository
{
    private readonly FlowCoreDbContext _db;

    public EfUserRepository(FlowCoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}
