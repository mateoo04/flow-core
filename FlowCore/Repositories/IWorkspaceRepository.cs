using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken ct = default);

    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
