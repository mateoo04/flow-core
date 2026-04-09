using FlowCore.Models;

namespace FlowCore.Data;

/// <summary>
/// LINQ patterns over the in-memory domain graph. Same shapes work later against IQueryable (EF Core) once you add a database.
/// </summary>
public static class DemoDataLinqExamples
{
    /// <summary>Flattens the tree: Workspace → Project → Board → Column → Task (classic <c>SelectMany</c> chain).</summary>
    public static IEnumerable<TaskItem> AllTasks(IEnumerable<Workspace> workspaces) =>
        workspaces
            .SelectMany(w => w.Projects)
            .SelectMany(p => p.Boards)
            .SelectMany(b => b.Columns)
            .SelectMany(c => c.Tasks);

    /// <summary>Top-level tasks only — filters with <c>Where</c> on FK.</summary>
    public static IEnumerable<TaskItem> RootTasksOnly(IEnumerable<Workspace> workspaces) =>
        AllTasks(workspaces).Where(t => t.ParentTaskItemId is null);

    /// <summary>Parents that have subtasks — uses navigation <c>Subtasks.Any()</c>.</summary>
    public static IEnumerable<TaskItem> ParentTasksWithSubtasks(IEnumerable<Workspace> workspaces) =>
        AllTasks(workspaces).Where(t => t.Subtasks.Count > 0);

    /// <summary>High or urgent priority — enum filter.</summary>
    public static IEnumerable<TaskItem> HotTasks(IEnumerable<Workspace> workspaces) =>
        AllTasks(workspaces).Where(t =>
            t.Priority is TaskPriority.High or TaskPriority.Urgent);

    /// <summary>Groups task counts by custom status name — <c>GroupBy</c> for dashboards.</summary>
    public static IEnumerable<(string StatusName, int Count)> TaskCountByStatus(IEnumerable<Workspace> workspaces) =>
        AllTasks(workspaces)
            .GroupBy(t => t.TaskStatusDefinition?.Name ?? "(unknown)")
            .Select(g => (StatusName: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count);

    /// <summary>
    /// Projects ranked by how many tasks they contain — <c>Select</c> with anonymous projection + <c>OrderByDescending</c>.
    /// </summary>
    public static IEnumerable<(string WorkspaceName, string ProjectKey, string ProjectName, int TaskCount)> ProjectsByTaskVolume(
        IEnumerable<Workspace> workspaces) =>
        workspaces
            .SelectMany(w => w.Projects.Select(p => (Workspace: w, Project: p)))
            .Select(x => (
                WorkspaceName: x.Workspace.Name,
                ProjectKey: x.Project.Key,
                ProjectName: x.Project.Name,
                TaskCount: x.Project.Boards.SelectMany(b => b.Columns).SelectMany(c => c.Tasks).Count()))
            .OrderByDescending(x => x.TaskCount);

    /// <summary>Same as <see cref="ProjectsByTaskVolume"/> but includes project id for dashboard links.</summary>
    public static IEnumerable<(Guid ProjectId, string WorkspaceName, string ProjectKey, string ProjectName, int TaskCount)>
        ProjectsByTaskVolumeWithIds(IEnumerable<Workspace> workspaces) =>
        workspaces
            .SelectMany(w => w.Projects.Select(p => (Workspace: w, Project: p)))
            .Select(x => (
                x.Project.Id,
                x.Workspace.Name,
                x.Project.Key,
                x.Project.Name,
                TaskCount: x.Project.Boards.SelectMany(b => b.Columns).SelectMany(c => c.Tasks).Count()))
            .OrderByDescending(x => x.TaskCount);

    /// <summary>
    /// Assignee name per task — nested <c>from</c> / <c>Join</c> on user id (pattern you will reuse for “my tasks” screens).
    /// </summary>
    public static IEnumerable<(string TaskTitle, string AssigneeName)> TaskTitlesWithAssignees(
        IEnumerable<Workspace> workspaces,
        IEnumerable<User> users) =>
        from task in AllTasks(workspaces) 
        from assignment in task.TaskAssignments
        join user in users on assignment.UserId equals user.Id
        where assignment.Role == TaskRole.Assignee
        select (task.Title, user.FullName);

    /// <summary>First task whose title contains a phrase — <c>FirstOrDefault</c> with predicate.</summary>
    public static TaskItem? FindTaskByTitleSubstring(IEnumerable<Workspace> workspaces, string substring) =>
        AllTasks(workspaces).FirstOrDefault(t => t.Title.Contains(substring, StringComparison.OrdinalIgnoreCase));

    /// <summary>Writes sample results to the console so the main program demonstrates LINQ without a UI yet.</summary>
    public static void WriteDevelopmentQuerySample(DemoDataGraph graph)
    {
        var workspaces = graph.Workspaces;

        Console.WriteLine("[LINQ] Root tasks (no parent): " + RootTasksOnly(workspaces).Count());
        Console.WriteLine("[LINQ] Parent tasks with subtasks: " + string.Join(", ",
            ParentTasksWithSubtasks(workspaces).Select(t => t.Title).Take(3)));
        Console.WriteLine("[LINQ] Hot tasks (High/Urgent): " + HotTasks(workspaces).Count());

        Console.WriteLine("[LINQ] Tasks by status:");
        foreach (var row in TaskCountByStatus(workspaces).Take(6))
            Console.WriteLine($"       {row.StatusName}: {row.Count}");

        Console.WriteLine("[LINQ] Busiest projects:");
        foreach (var row in ProjectsByTaskVolume(workspaces).Take(5))
            Console.WriteLine($"       {row.WorkspaceName} / {row.ProjectKey} ({row.ProjectName}): {row.TaskCount} tasks");

        Console.WriteLine("[LINQ] Sample assignee rows:");
        foreach (var row in TaskTitlesWithAssignees(workspaces, graph.Users).Take(4))
            Console.WriteLine($"       {row.TaskTitle} → {row.AssigneeName}");

        var hero = FindTaskByTitleSubstring(workspaces, "hero");
        Console.WriteLine("[LINQ] Find title containing 'hero': " + (hero?.Title ?? "(none)"));
    }
}
