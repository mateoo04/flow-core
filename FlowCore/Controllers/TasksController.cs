using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class TasksController : BaseController
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly ICommentRepository _comments;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;
    private readonly InMemoryDataStore _store;

    public TasksController(
        ITaskRepository tasks,
        IProjectRepository projects,
        ICommentRepository comments,
        IBreadcrumbTrailBuilder breadcrumbs,
        InMemoryDataStore store)
    {
        _tasks = tasks;
        _projects = projects;
        _comments = comments;
        _breadcrumbs = breadcrumbs;
        _store = store;
    }

    public IActionResult Index()
    {
        var rows = _tasks.GetAll()
            .Select(t => new TaskListRow(t.Id, t.Title, t.Priority, t.BoardId, t.ParentTaskItemId))
            .OrderBy(r => r.Title)
            .ToList();
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create(Guid projectId, Guid? boardId, Guid? parentTaskItemId)
    {
        var project = _projects.GetById(projectId);
        if (project is null)
            return NotFound();

        var board = boardId is { } bid
            ? project.Boards.FirstOrDefault(b => b.Id == bid)
            : null;
        board ??= project.Boards.OrderBy(b => b.Position).FirstOrDefault(b => b.IsDefault);
        board ??= project.Boards.OrderBy(b => b.Position).FirstOrDefault();
        if (board is null)
            return NotFound();

        var workspace = project.Workspace ?? _store.FindWorkspace(project.WorkspaceId);
        if (workspace is null)
            return NotFound();
        var statuses = workspace.TaskStatusDefinitions.OrderBy(s => s.Position).ToList();
        var defaultStatus = statuses.FirstOrDefault();
        if (defaultStatus is null)
            return NotFound();

        SetNav(project.WorkspaceId, project.Id);

        var vm = new TaskCreateFormVm
        {
            ProjectId = projectId,
            BoardId = board.Id,
            TaskStatusDefinitionId = defaultStatus.Id,
            ParentTaskItemId = parentTaskItemId
        };
        ViewBag.Project = project;
        ViewBag.Board = board;
        ViewBag.Statuses = statuses;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TaskCreateFormVm model)
    {
        var project = _projects.GetById(model.ProjectId);
        if (project is null)
            return NotFound();

        var workspace = project.Workspace ?? _store.FindWorkspace(project.WorkspaceId);
        var statuses = workspace?.TaskStatusDefinitions.OrderBy(s => s.Position).ToList()
                       ?? new List<TaskStatusDefinition>();

        if (!ModelState.IsValid)
        {
            var board = project.Boards.FirstOrDefault(b => b.Id == model.BoardId)
                        ?? project.Boards.OrderBy(b => b.Position).First();
            ViewBag.Project = project;
            ViewBag.Board = board;
            ViewBag.Statuses = statuses;
            return View(model);
        }

        var req = new CreateTaskRequest(
            model.BoardId,
            model.TaskStatusDefinitionId,
            model.Title,
            model.Description,
            model.Priority,
            model.StoryPoints,
            model.ParentTaskItemId,
            model.DueDate);

        var task = _tasks.Create(req);
        if (task is null)
        {
            ModelState.AddModelError(string.Empty, "Could not create task.");
            var board = project.Boards.FirstOrDefault(b => b.Id == model.BoardId)
                        ?? project.Boards.OrderBy(b => b.Position).First();
            ViewBag.Project = project;
            ViewBag.Board = board;
            ViewBag.Statuses = statuses;
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = task.Id });
    }

    public IActionResult Details(Guid id)
    {
        var entity = _tasks.GetById(id);
        var project = entity?.Board?.Project;
        if (project is not null)
            SetNav(project.WorkspaceId, project.Id);
        return ViewDetails(entity, _breadcrumbs.ForTask);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddComment(Guid id, CommentFormVm model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Details), new { id });

        var comment = _comments.Create(id, DemoSeedIds.UserAlex, model.Body);
        if (comment is null)
            return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        var entity = _tasks.GetById(id);
        if (entity is null)
            return NotFound();

        var projectId = entity.Board?.ProjectId;
        if (!_tasks.TryDelete(id))
            return NotFound();

        if (projectId is { } pid)
            return RedirectToAction(nameof(ProjectsController.Details), "Projects", new { id = pid });

        return RedirectToAction(nameof(Index));
    }
}
