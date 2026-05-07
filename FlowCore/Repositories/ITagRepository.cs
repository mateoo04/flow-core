using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken ct = default);

    Task<Tag?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
