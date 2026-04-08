using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
{
    private readonly IReadOnlyList<Workspace> _all;
    private readonly Dictionary<Guid, Workspace> _byId;

    public InMemoryWorkspaceRepository(DemoDataGraph graph)
    {
        _all = graph.Workspaces.ToList();
        _byId = _all.ToDictionary(w => w.Id);
    }

    public IReadOnlyList<Workspace> GetAll() => _all;

    public Workspace? GetById(Guid id) =>
        _byId.TryGetValue(id, out var w) ? w : null;
}
