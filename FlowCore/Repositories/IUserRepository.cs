using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
