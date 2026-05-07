using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TaskItem>> GetByBoardIdAsync(Guid boardId, CancellationToken ct = default);

    Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(Guid userId, CancellationToken ct = default);

    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct = default);

    Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default);
}
