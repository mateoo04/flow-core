using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryWorkspaceRepository(InMemoryDataStore store) => _store = store;

    public IReadOnlyList<Workspace> GetAll()
    {
        lock (_store.Sync)
            return _store.Workspaces.ToList();
    }

    public Workspace? GetById(Guid id)
    {
        lock (_store.Sync)
            return _store.FindWorkspace(id);
    }
}
