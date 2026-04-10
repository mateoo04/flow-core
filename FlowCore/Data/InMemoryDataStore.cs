using FlowCore.Models;

namespace FlowCore.Data;

/// <summary>
/// Mutable in-memory graph seeded from demo data. All in-memory repositories share this instance.
/// </summary>
public sealed class InMemoryDataStore
{
    internal object Sync { get; } = new();

    public List<Workspace> Workspaces { get; }
    public List<User> Users { get; }
    public List<Tag> Tags { get; }

    public InMemoryDataStore(DemoDataGraph seed)
    {
        Workspaces = seed.Workspaces.ToList();
        Users = seed.Users.ToList();
        Tags = seed.Tags.ToList();
    }

    public TaskItem? FindTask(Guid id)
    {
        return DemoDataLinqExamples.AllTasks(Workspaces).FirstOrDefault(t => t.Id == id);
    }

    public Project? FindProject(Guid id) =>
        Workspaces.SelectMany(w => w.Projects).FirstOrDefault(p => p.Id == id);

    public Workspace? FindWorkspace(Guid id) =>
        Workspaces.FirstOrDefault(w => w.Id == id);

    public BoardColumn? FindBoardColumn(Guid id) =>
        Workspaces
            .SelectMany(w => w.Projects)
            .SelectMany(p => p.Boards)
            .SelectMany(b => b.Columns)
            .FirstOrDefault(c => c.Id == id);

    public TaskStatusDefinition? FindTaskStatusDefinition(Guid id) =>
        Workspaces
            .SelectMany(w => w.Projects)
            .SelectMany(p => p.TaskStatusDefinitions)
            .FirstOrDefault(s => s.Id == id);
}
