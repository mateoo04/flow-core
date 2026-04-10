using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryBoardRepository : IBoardRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryBoardRepository(InMemoryDataStore store) => _store = store;

    public IReadOnlyList<Board> GetAll()
    {
        lock (_store.Sync)
            return _store.Workspaces
                .SelectMany(w => w.Projects)
                .SelectMany(p => p.Boards)
                .ToList();
    }

    public IReadOnlyList<Board> GetByProjectId(Guid projectId)
    {
        lock (_store.Sync)
            return _store.FindProject(projectId)?.Boards.ToList()
                   ?? (IReadOnlyList<Board>)Array.Empty<Board>();
    }

    public Board? GetById(Guid id)
    {
        lock (_store.Sync)
            return _store.Workspaces
                .SelectMany(w => w.Projects)
                .SelectMany(p => p.Boards)
                .FirstOrDefault(b => b.Id == id);
    }
}
