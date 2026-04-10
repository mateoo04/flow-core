using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryTaskStatusDefinitionRepository : ITaskStatusDefinitionRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryTaskStatusDefinitionRepository(InMemoryDataStore store) => _store = store;

    public IReadOnlyList<TaskStatusDefinition> GetAll()
    {
        lock (_store.Sync)
            return _store.Workspaces
                .SelectMany(w => w.Projects)
                .SelectMany(p => p.TaskStatusDefinitions)
                .ToList();
    }

    public IReadOnlyList<TaskStatusDefinition> GetByProjectId(Guid projectId)
    {
        lock (_store.Sync)
            return _store.FindProject(projectId)?.TaskStatusDefinitions
                       .OrderBy(s => s.Position)
                       .ToList()
                   ?? (IReadOnlyList<TaskStatusDefinition>)Array.Empty<TaskStatusDefinition>();
    }

    public TaskStatusDefinition? GetById(Guid id)
    {
        lock (_store.Sync)
            return _store.FindTaskStatusDefinition(id);
    }

    public TaskStatusDefinition? Add(
        Guid projectId,
        string name,
        string colorHex,
        bool isDefault,
        bool isDoneState)
    {
        lock (_store.Sync)
        {
            var project = _store.FindProject(projectId);
            if (project is null || string.IsNullOrWhiteSpace(name))
                return null;

            var positions = project.TaskStatusDefinitions.Select(s => s.Position).ToList();
            var next = positions.Count == 0 ? 0 : positions.Max() + 1;

            var sd = new TaskStatusDefinition
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = name.Trim(),
                ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#94A3B8" : colorHex.Trim(),
                Position = next,
                IsDefault = isDefault,
                IsDoneState = isDoneState,
                CreatedAt = DateTime.UtcNow,
                Project = project
            };

            if (isDefault)
            {
                foreach (var s in project.TaskStatusDefinitions)
                    s.IsDefault = false;
            }

            project.TaskStatusDefinitions.Add(sd);
            return sd;
        }
    }

    public bool TryDelete(Guid id, Guid? reassignToStatusId)
    {
        lock (_store.Sync)
        {
            var sd = _store.FindTaskStatusDefinition(id);
            if (sd?.Project is null)
                return false;

            var project = sd.Project;
            if (project.TaskStatusDefinitions.Count <= 1)
                return false;
            var affected = sd.TaskItems.ToList();
            if (affected.Count > 0)
            {
                if (reassignToStatusId is null || reassignToStatusId == id)
                    return false;

                var replacement = project.TaskStatusDefinitions.FirstOrDefault(s => s.Id == reassignToStatusId);
                if (replacement is null)
                    return false;

                foreach (var task in affected)
                {
                    task.TaskStatusDefinition?.TaskItems.Remove(task);
                    task.TaskStatusDefinitionId = replacement.Id;
                    task.TaskStatusDefinition = replacement;
                    replacement.TaskItems.Add(task);
                }
            }

            if (sd.IsDefault)
            {
                var other = project.TaskStatusDefinitions.FirstOrDefault(s => s.Id != id);
                if (other is not null)
                    other.IsDefault = true;
            }

            project.TaskStatusDefinitions.Remove(sd);
            sd.Project = null;
            return true;
        }
    }
}
