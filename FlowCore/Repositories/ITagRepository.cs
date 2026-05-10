using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken ct = default);

    Task<Tag?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Tag> AddAsync(Tag tag, CancellationToken ct = default);

    Task<Tag?> UpdateAsync(Guid id, string name, string colorHex, CancellationToken ct = default);

    Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default);

    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default);
}
