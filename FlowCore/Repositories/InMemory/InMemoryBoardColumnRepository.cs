using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryBoardColumnRepository : IBoardColumnRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryBoardColumnRepository(InMemoryDataStore store) => _store = store;

    public IReadOnlyList<BoardColumn> GetAll()
    {
        lock (_store.Sync)
            return _store.Workspaces
                .SelectMany(w => w.Projects)
                .SelectMany(p => p.Boards)
                .SelectMany(b => b.Columns)
                .ToList();
    }

    public IReadOnlyList<BoardColumn> GetByBoardId(Guid boardId)
    {
        lock (_store.Sync)
            return _store.Workspaces
                .SelectMany(w => w.Projects)
                .SelectMany(p => p.Boards)
                .Where(b => b.Id == boardId)
                .SelectMany(b => b.Columns)
                .OrderBy(c => c.Position)
                .ToList();
    }

    public BoardColumn? GetById(Guid id)
    {
        lock (_store.Sync)
            return _store.FindBoardColumn(id);
    }
}
