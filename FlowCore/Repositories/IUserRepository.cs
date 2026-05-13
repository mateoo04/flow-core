using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<User>> SearchActiveAsync(
        string query,
        IReadOnlyCollection<Guid> excludeIds,
        int take,
        CancellationToken ct = default);

    Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<User> AddAsync(User user, CancellationToken ct = default);

    Task<User?> UpdateAsync(Guid id, string fullName, string email, bool isActive, CancellationToken ct = default);

    Task<User?> DeactivateAsync(Guid id, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct = default);

    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
}
