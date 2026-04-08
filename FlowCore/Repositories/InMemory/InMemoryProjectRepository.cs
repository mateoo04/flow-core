using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly IReadOnlyList<Project> _all;
    private readonly Dictionary<Guid, Project> _byId;

    public InMemoryProjectRepository(DemoDataGraph graph)
    {
        _all = graph.Workspaces.SelectMany(w => w.Projects).ToList();
        _byId = _all.ToDictionary(p => p.Id);
    }

    public IReadOnlyList<Project> GetAll() => _all;

    public IReadOnlyList<Project> GetByWorkspaceId(Guid workspaceId) =>
        _all.Where(p => p.WorkspaceId == workspaceId).ToList();

    public Project? GetById(Guid id) =>
        _byId.TryGetValue(id, out var p) ? p : null;
}
