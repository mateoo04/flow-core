using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken ct = default);

    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default);

    Task<Workspace?> UpdateAsync(
        Guid id,
        string name,
        string description,
        WorkspaceVisibility visibility,
        CancellationToken ct = default);

    Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default);

    Task<bool> HasProjectsAsync(Guid workspaceId, CancellationToken ct = default);

    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default);
}
