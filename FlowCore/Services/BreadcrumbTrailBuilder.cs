using FlowCore.Models;
using FlowCore.Models.ViewModels;

namespace FlowCore.Services;

public interface IBreadcrumbTrailBuilder
{
    IReadOnlyList<BreadcrumbItem> ForWorkspace(Workspace w);

    IReadOnlyList<BreadcrumbItem> ForProject(Project p);

    IReadOnlyList<BreadcrumbItem> ForBoard(Board b);

    IReadOnlyList<BreadcrumbItem> ForBoardColumn(BoardColumn c);

    IReadOnlyList<BreadcrumbItem> ForTask(TaskItem t);

    IReadOnlyList<BreadcrumbItem> ForTaskStatusDefinition(TaskStatusDefinition s);

    IReadOnlyList<BreadcrumbItem> ForUser(User u);

    IReadOnlyList<BreadcrumbItem> ForTag(Tag t);

    IReadOnlyList<BreadcrumbItem> ForComment(Comment c, string taskTitle);
}

/// <summary>
/// Builds linked trails using conventional MVC paths (<c>/Controller/Action/id</c>).
/// </summary>
public sealed class BreadcrumbTrailBuilder : IBreadcrumbTrailBuilder
{
    private static BreadcrumbItem Home() => new("Home", "/Home/Index");

    public IReadOnlyList<BreadcrumbItem> ForWorkspace(Workspace w) =>
    [
        Home(),
        new("Organizations", "/Workspaces/Index"),
        new(w.Name, null)
    ];

    public IReadOnlyList<BreadcrumbItem> ForProject(Project p)
    {
        var wsName = p.Workspace?.Name ?? "Organization";
        return
        [
            Home(),
            new("Organizations", "/Workspaces/Index"),
            new(wsName, $"/Workspaces/Details/{p.WorkspaceId:D}"),
            new("Projects", "/Projects/Index"),
            new($"{p.Key} — {p.Name}", null)
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForBoard(Board b)
    {
        var project = b.Project;
        if (project?.Workspace is null)
            return [Home(), new("Boards", "/Boards/Index"), new(b.Name, null)];

        var ws = project.Workspace;
        return
        [
            Home(),
            new("Organizations", "/Workspaces/Index"),
            new(ws.Name, $"/Workspaces/Details/{ws.Id:D}"),
            new("Projects", "/Projects/Index"),
            new(project.Key, $"/Projects/Details/{project.Id:D}"),
            new("Boards", "/Boards/Index"),
            new(b.Name, null)
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForBoardColumn(BoardColumn c)
    {
        var board = c.Board;
        if (board?.Project?.Workspace is null)
            return [Home(), new("Columns", "/BoardColumns/Index"), new(c.Name, null)];

        var project = board.Project!;
        var ws = project.Workspace!;
        return
        [
            Home(),
            new("Organizations", "/Workspaces/Index"),
            new(ws.Name, $"/Workspaces/Details/{ws.Id:D}"),
            new("Projects", "/Projects/Index"),
            new(project.Key, $"/Projects/Details/{project.Id:D}"),
            new("Boards", "/Boards/Index"),
            new(board.Name, $"/Boards/Details/{board.Id:D}"),
            new("Columns", "/BoardColumns/Index"),
            new(c.Name, null)
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForTask(TaskItem t)
    {
        var col = t.BoardColumn;
        var board = col?.Board;
        var project = board?.Project;
        var ws = project?.Workspace;

        if (ws is null || project is null || board is null || col is null)
            return [Home(), new("Tasks", "/Tasks/Index"), new(t.Title, null)];

        return
        [
            Home(),
            new("Organizations", "/Workspaces/Index"),
            new(ws.Name, $"/Workspaces/Details/{ws.Id:D}"),
            new("Projects", "/Projects/Index"),
            new(project.Key, $"/Projects/Details/{project.Id:D}"),
            new("Boards", "/Boards/Index"),
            new(board.Name, $"/Boards/Details/{board.Id:D}"),
            new("Columns", "/BoardColumns/Index"),
            new(col.Name, $"/BoardColumns/Details/{col.Id:D}"),
            new("Tasks", "/Tasks/Index"),
            new(t.Title, null)
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForTaskStatusDefinition(TaskStatusDefinition s)
    {
        var project = s.Project;
        if (project?.Workspace is null)
            return [Home(), new("Statuses", "/TaskStatusDefinitions/Index"), new(s.Name, null)];

        var ws = project.Workspace;
        return
        [
            Home(),
            new("Organizations", "/Workspaces/Index"),
            new(ws.Name, $"/Workspaces/Details/{ws.Id:D}"),
            new("Projects", "/Projects/Index"),
            new(project.Key, $"/Projects/Details/{project.Id:D}"),
            new("Statuses", "/TaskStatusDefinitions/Index"),
            new(s.Name, null)
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForUser(User u) =>
    [
        Home(),
        new("Users", "/Users/Index"),
        new(u.FullName, null)
    ];

    public IReadOnlyList<BreadcrumbItem> ForTag(Tag t) =>
    [
        Home(),
        new("Tags", "/Tags/Index"),
        new(t.Name, null)
    ];

    public IReadOnlyList<BreadcrumbItem> ForComment(Comment c, string taskTitle) =>
    [
        Home(),
        new("Comments", "/Comments/Index"),
        new(taskTitle, $"/Tasks/Details/{c.TaskItemId:D}"),
        new("Comment", null)
    ];
}
