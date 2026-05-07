using FlowCore.Models;
using FlowCore.Models.ViewModels;
using Microsoft.AspNetCore.Routing;

namespace FlowCore.Services;

public interface IBreadcrumbTrailBuilder
{
    IReadOnlyList<BreadcrumbItem> ForWorkspace(Workspace w);

    IReadOnlyList<BreadcrumbItem> ForProject(Project p);

    IReadOnlyList<BreadcrumbItem> ForBoard(Board b);

    IReadOnlyList<BreadcrumbItem> ForTask(TaskItem t);

    IReadOnlyList<BreadcrumbItem> ForUser(User u);

    IReadOnlyList<BreadcrumbItem> ForTag(Tag t);

    IReadOnlyList<BreadcrumbItem> ForComment(Comment c, string taskTitle);
}

public sealed class BreadcrumbTrailBuilder : IBreadcrumbTrailBuilder
{
    private readonly LinkGenerator _links;

    public BreadcrumbTrailBuilder(LinkGenerator links) => _links = links;

    private string? Path(string controller, string action, object? values = null) =>
        _links.GetPathByAction(action, controller, values);

    private BreadcrumbItem ProjectsIndex() => new("Projects", Path("Projects", "Index"));

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
            return [new("Boards", Path("Boards", "Index")), new(b.Name, null)];

        return
        [
            ProjectsIndex(),
            new(project.Name, Path("Projects", "Details", new { id = project.Id })),
            new(b.Name, null)
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForTask(TaskItem t)
    {
        var board = t.Board;
        var project = board?.Project;

        if (project is null || board is null)
            return [new("Tasks", Path("Tasks", "Index"))];

        return
        [
            ProjectsIndex(),
            new(project.Name, Path("Projects", "Details", new { id = project.Id })),
            new(board.Name, Path("Projects", "Details", new { id = project.Id, boardId = board.Id }))
        ];
    }

    public IReadOnlyList<BreadcrumbItem> ForUser(User u) =>
    [
        new("Users", Path("Users", "Index")),
        new(u.FullName, null)
    ];

    public IReadOnlyList<BreadcrumbItem> ForTag(Tag t) =>
    [
        new("Tags", Path("Tags", "Index")),
        new(t.Name, null)
    ];

    public IReadOnlyList<BreadcrumbItem> ForComment(Comment c, string taskTitle) =>
    [
        new("Comments", Path("Comments", "Index")),
        new("Comment", null)
    ];
}
