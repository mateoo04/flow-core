using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Project>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);

    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Project> AddAsync(Project project, CancellationToken ct = default);

    Task<Project?> UpdateAsync(
        Guid id,
        string name,
        string description,
        ProjectStatus status,
        ProjectPriority priority,
        DateTime? startDate,
        DateTime? dueDate,
        CancellationToken ct = default);

    Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default);
}
