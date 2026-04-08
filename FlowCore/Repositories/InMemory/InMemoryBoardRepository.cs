using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryBoardRepository : IBoardRepository
{
    private readonly IReadOnlyList<Board> _all;
    private readonly Dictionary<Guid, Board> _byId;

    public InMemoryBoardRepository(DemoDataGraph graph)
    {
        _all = graph.Workspaces
            .SelectMany(w => w.Projects)
            .SelectMany(p => p.Boards)
            .ToList();
        _byId = _all.ToDictionary(b => b.Id);
    }

    public IReadOnlyList<Board> GetAll() => _all;

    public IReadOnlyList<Board> GetByProjectId(Guid projectId) =>
        _all.Where(b => b.ProjectId == projectId).ToList();

    public Board? GetById(Guid id) =>
        _byId.TryGetValue(id, out var b) ? b : null;
}
