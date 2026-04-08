using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryTaskStatusDefinitionRepository : ITaskStatusDefinitionRepository
{
    private readonly IReadOnlyList<TaskStatusDefinition> _all;
    private readonly Dictionary<Guid, TaskStatusDefinition> _byId;

    public InMemoryTaskStatusDefinitionRepository(DemoDataGraph graph)
    {
        _all = graph.Workspaces
            .SelectMany(w => w.Projects)
            .SelectMany(p => p.TaskStatusDefinitions)
            .ToList();
        _byId = _all.ToDictionary(s => s.Id);
    }

    public IReadOnlyList<TaskStatusDefinition> GetAll() => _all;

    public IReadOnlyList<TaskStatusDefinition> GetByProjectId(Guid projectId) =>
        _all.Where(s => s.ProjectId == projectId).OrderBy(s => s.Position).ToList();

    public TaskStatusDefinition? GetById(Guid id) =>
        _byId.TryGetValue(id, out var s) ? s : null;
}
