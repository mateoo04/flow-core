using FlowCore.Models;

namespace FlowCore.Data;

public static class DemoDataLinqExamples
{
    public static IEnumerable<TaskItem> AllTasks(IEnumerable<Workspace> workspaces) =>
        workspaces
            .SelectMany(w => w.Projects)
            .SelectMany(p => p.Boards)
            .SelectMany(b => b.Columns)
            .SelectMany(c => c.Tasks);

    public static IEnumerable<TaskItem> RootTasksOnly(IEnumerable<Workspace> workspaces) =>
        AllTasks(workspaces).Where(t => t.ParentTaskItemId is null);

    public static IEnumerable<TaskItem> ParentTasksWithSubtasks(IEnumerable<Workspace> workspaces) =>
        AllTasks(workspaces).Where(t => t.Subtasks.Count > 0);

    public static IEnumerable<TaskItem> HotTasks(IEnumerable<Workspace> workspaces) =>
        AllTasks(workspaces).Where(t =>
            t.Priority is TaskPriority.High or TaskPriority.Urgent);

    public static IEnumerable<(string StatusName, int Count)> TaskCountByStatus(IEnumerable<Workspace> workspaces) =>
        AllTasks(workspaces)
            .GroupBy(t => t.TaskStatusDefinition?.Name ?? "(unknown)")
            .Select(g => (StatusName: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count);

    public static IEnumerable<(string WorkspaceName, string ProjectName, int TaskCount)> ProjectsByTaskVolume(
        IEnumerable<Workspace> workspaces) =>
        workspaces
            .SelectMany(w => w.Projects.Select(p => (Workspace: w, Project: p)))
            .Select(x => (
                WorkspaceName: x.Workspace.Name,
                ProjectName: x.Project.Name,
                TaskCount: x.Project.Boards.SelectMany(b => b.Columns).SelectMany(c => c.Tasks).Count()))
            .OrderByDescending(x => x.TaskCount);

    public static IEnumerable<(Guid ProjectId, string WorkspaceName, string ProjectName, int TaskCount)>
        ProjectsByTaskVolumeWithIds(IEnumerable<Workspace> workspaces) =>
        workspaces
            .SelectMany(w => w.Projects.Select(p => (Workspace: w, Project: p)))
            .Select(x => (
                x.Project.Id,
                x.Workspace.Name,
                x.Project.Name,
                TaskCount: x.Project.Boards.SelectMany(b => b.Columns).SelectMany(c => c.Tasks).Count()))
            .OrderByDescending(x => x.TaskCount);

    public static IEnumerable<(string TaskTitle, string AssigneeName)> TaskTitlesWithAssignees(
        IEnumerable<Workspace> workspaces,
        IEnumerable<User> users) =>
        from task in AllTasks(workspaces)
        from assignment in task.TaskAssignments
        join user in users on assignment.UserId equals user.Id
        where assignment.Role == TaskRole.Assignee
        select (task.Title, user.FullName);

    public static TaskItem? FindTaskByTitleSubstring(IEnumerable<Workspace> workspaces, string substring) =>
        AllTasks(workspaces).FirstOrDefault(t => t.Title.Contains(substring, StringComparison.OrdinalIgnoreCase));

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
            Console.WriteLine($"       {row.WorkspaceName} / {row.ProjectName}: {row.TaskCount} tasks");

        Console.WriteLine("[LINQ] Sample assignee rows:");
        foreach (var row in TaskTitlesWithAssignees(workspaces, graph.Users).Take(4))
            Console.WriteLine($"       {row.TaskTitle} → {row.AssigneeName}");

        var hero = FindTaskByTitleSubstring(workspaces, "hero");
        Console.WriteLine("[LINQ] Find title containing 'hero': " + (hero?.Title ?? "(none)"));
    }
}
