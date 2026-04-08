using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly IReadOnlyList<TaskItem> _all;
    private readonly Dictionary<Guid, TaskItem> _byId;

    public InMemoryTaskRepository(DemoDataGraph graph)
    {
        _all = DemoDataLinqExamples.AllTasks(graph.Workspaces).ToList();
        _byId = _all.ToDictionary(t => t.Id);
    }

    public IReadOnlyList<TaskItem> GetAll() => _all;

    public IReadOnlyList<TaskItem> GetByBoardColumnId(Guid boardColumnId) =>
        _all.Where(t => t.BoardColumnId == boardColumnId).ToList();

    public TaskItem? GetById(Guid id) =>
        _byId.TryGetValue(id, out var t) ? t : null;
}
