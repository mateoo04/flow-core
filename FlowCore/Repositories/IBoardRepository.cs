using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IBoardRepository
{
    Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Board>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);

    Task<Board?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
