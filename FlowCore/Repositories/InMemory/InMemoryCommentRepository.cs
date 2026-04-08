using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryCommentRepository : ICommentRepository
{
    private readonly IReadOnlyList<Comment> _all;
    private readonly Dictionary<Guid, Comment> _byId;

    public InMemoryCommentRepository(DemoDataGraph graph)
    {
        _all = DemoDataLinqExamples.AllTasks(graph.Workspaces)
            .SelectMany(t => t.Comments)
            .ToList();
        _byId = _all.ToDictionary(c => c.Id);
    }

    public IReadOnlyList<Comment> GetAll() => _all;

    public IReadOnlyList<Comment> GetByTaskItemId(Guid taskItemId) =>
        _all.Where(c => c.TaskItemId == taskItemId).OrderBy(c => c.CreatedAt).ToList();

    public Comment? GetById(Guid id) =>
        _byId.TryGetValue(id, out var c) ? c : null;
}
