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

public sealed class BreadcrumbTrailBuilder : IBreadcrumbTrailBuilder
{
    private static BreadcrumbItem ProjectsIndex() => new("Projects", "/Projects/Index");

    public IReadOnlyList<BreadcrumbItem> ForWorkspace(Workspace w) =>
        Array.Empty<BreadcrumbItem>();

    public IReadOnlyList<BreadcrumbItem> ForProject(Project p) =>
    [
        ProjectsIndex(),
        new(p.Name, null)
    ];

    public IReadOnlyList<BreadcrumbItem> ForBoard(Board b)
    {
        var project = b.Project;
        if (project is null)
            return [new("Boards", "/Boards/Index"), new(b.Name, null)];

        return
        [
            ProjectsIndex(),
            new(project.Name, $"/Projects/Details/{project.Id:D}"),
            new(b.Name, null)
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForBoardColumn(BoardColumn c)
    {
        var board = c.Board;
        var project = board?.Project;
        if (project is null)
            return [new("Columns", "/BoardColumns/Index"), new("Column", null)];

        return
        [
            ProjectsIndex(),
            new(project.Name, $"/Projects/Details/{project.Id:D}"),
            new(board!.Name, $"/Projects/Details/{project.Id:D}?boardId={board.Id:D}")
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForTask(TaskItem t)
    {
        var col = t.BoardColumn;
        var board = col?.Board;
        var project = board?.Project;

        if (project is null || board is null)
            return [new("Tasks", "/Tasks/Index")];

        return
        [
            ProjectsIndex(),
            new(project.Name, $"/Projects/Details/{project.Id:D}"),
            new(board.Name, $"/Projects/Details/{project.Id:D}?boardId={board.Id:D}")
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForTaskStatusDefinition(TaskStatusDefinition s)
    {
        var project = s.Project;
        if (project is null)
            return [new("Statuses", "/TaskStatusDefinitions/Index"), new(s.Name, null)];

        return
        [
            ProjectsIndex(),
            new(project.Name, $"/Projects/Details/{project.Id:D}"),
            new(s.Name, null)
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForUser(User u) =>
    [
        new("Users", "/Users/Index"),
        new(u.FullName, null)
    ];

    public IReadOnlyList<BreadcrumbItem> ForTag(Tag t) =>
    [
        new("Tags", "/Tags/Index"),
        new(t.Name, null)
    ];

    public IReadOnlyList<BreadcrumbItem> ForComment(Comment c, string taskTitle) =>
    [
        new("Comments", "/Comments/Index"),
        new("Comment", null)
    ];
}
