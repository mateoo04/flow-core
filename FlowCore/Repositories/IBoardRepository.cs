using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IBoardRepository
{
    Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Board>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);

    Task<Board?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Board> AddAsync(Guid projectId, string name, bool isDefault, CancellationToken ct = default);

    Task<Board?> UpdateAsync(Guid id, string name, bool isDefault, CancellationToken ct = default);

    Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default);

    Task<bool> NameExistsInProjectAsync(Guid projectId, string name, Guid? excludeId, CancellationToken ct = default);
}
