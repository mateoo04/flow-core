using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryProjectRepository(InMemoryDataStore store) => _store = store;

    public IReadOnlyList<Project> GetAll()
    {
        lock (_store.Sync)
            return _store.Workspaces.SelectMany(w => w.Projects).ToList();
    }

    public IReadOnlyList<Project> GetByWorkspaceId(Guid workspaceId)
    {
        lock (_store.Sync)
            return _store.Workspaces
                .Where(w => w.Id == workspaceId)
                .SelectMany(w => w.Projects)
                .ToList();
    }

    public Project? GetById(Guid id)
    {
        lock (_store.Sync)
            return _store.FindProject(id);
    }

    public Project CreateInWorkspace(
        Guid workspaceId,
        string name,
        string description,
        ProjectStatus status,
        ProjectPriority priority)
    {
        lock (_store.Sync)
        {
            var ws = _store.FindWorkspace(workspaceId)
                     ?? throw new InvalidOperationException("Workspace not found.");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));

            var ordered = ws.TaskStatusDefinitions.OrderBy(s => s.Position).ToList();
            if (ordered.Count < 4)
                throw new InvalidOperationException("This workspace needs at least four task statuses before a project can be created.");

            var workspaceStatuses = new WorkspaceStatuses
            {
                Backlog = ordered[0],
                Todo = ordered.Count > 1 ? ordered[1] : ordered[0],
                InProgress = ordered.Count > 2 ? ordered[2] : ordered[0],
                Done = ordered.LastOrDefault(s => s.IsDoneState) ?? ordered[^1]
            };

            var ctx = ProjectBlueprint.CreateProject(
                ws,
                Guid.NewGuid,
                DateTime.UtcNow,
                workspaceStatuses,
                name.Trim(),
                description.Trim(),
                status,
                priority);
            ws.Projects.Add(ctx.Project);
            return ctx.Project;
        }
    }

    public bool TryDelete(Guid id)
    {
        lock (_store.Sync)
        {
            var project = _store.FindProject(id);
            if (project?.Workspace is null)
                return false;

            var inProject = project.Boards
                .SelectMany(b => b.Tasks)
                .ToList();

            foreach (var root in inProject.Where(t => t.ParentTaskItemId is null).ToList())
                TaskGraphRemoval.RemoveTaskRecursive(_store, root);

            project.Workspace.Projects.Remove(project);
            return true;
        }
    }
}
