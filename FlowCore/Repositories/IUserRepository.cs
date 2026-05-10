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
}
