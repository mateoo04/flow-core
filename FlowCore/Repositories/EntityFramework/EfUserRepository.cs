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

    public async Task<IReadOnlyList<User>> SearchActiveAsync(
        string query,
        IReadOnlyCollection<Guid> excludeIds,
        int take,
        CancellationToken ct = default)
    {
        var pattern = $"%{query}%";
        var q = _db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .Where(u => EF.Functions.ILike(u.FullName, pattern)
                     || EF.Functions.ILike(u.Email, pattern));

        if (excludeIds.Count > 0)
            q = q.Where(u => !excludeIds.Contains(u.Id));

        return await q.OrderBy(u => u.FullName).Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return Array.Empty<User>();
        return await _db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(ct);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User?> UpdateAsync(Guid id, string fullName, string email, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;
        user.FullName = fullName;
        user.Email = email;
        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User?> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;
        user.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct = default)
    {
        var lowered = email.Trim().ToLowerInvariant();
        return _db.Users.AnyAsync(u =>
            u.Email.ToLower() == lowered && (excludeId == null || u.Id != excludeId), ct);
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);
    }
}
