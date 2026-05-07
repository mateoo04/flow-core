using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IStatusRepository
{
    Task<IReadOnlyList<TaskStatusDefinition>> GetAllAsync(CancellationToken ct = default);
}
