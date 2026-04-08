using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryBoardColumnRepository : IBoardColumnRepository
{
    private readonly IReadOnlyList<BoardColumn> _all;
    private readonly Dictionary<Guid, BoardColumn> _byId;

    public InMemoryBoardColumnRepository(DemoDataGraph graph)
    {
        _all = graph.Workspaces
            .SelectMany(w => w.Projects)
            .SelectMany(p => p.Boards)
            .SelectMany(b => b.Columns)
            .ToList();
        _byId = _all.ToDictionary(c => c.Id);
    }

    public IReadOnlyList<BoardColumn> GetAll() => _all;

    public IReadOnlyList<BoardColumn> GetByBoardId(Guid boardId) =>
        _all.Where(c => c.BoardId == boardId).OrderBy(c => c.Position).ToList();

    public BoardColumn? GetById(Guid id) =>
        _byId.TryGetValue(id, out var c) ? c : null;
}
