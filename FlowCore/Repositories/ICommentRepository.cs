using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ICommentRepository
{
    Task<IReadOnlyList<Comment>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Comment>> GetByTaskItemIdAsync(Guid taskItemId, CancellationToken ct = default);

    Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Comment> AddAsync(Comment comment, CancellationToken ct = default);

    Task<Comment?> UpdateBodyAsync(Guid id, string body, CancellationToken ct = default);

    Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default);
}
