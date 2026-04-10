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

    public TasksController(
        ITaskRepository tasks,
        IProjectRepository projects,
        ICommentRepository comments,
        IBreadcrumbTrailBuilder breadcrumbs)
    {
        _tasks = tasks;
        _projects = projects;
        _comments = comments;
        _breadcrumbs = breadcrumbs;
    }

    public IActionResult Index()
    {
        var rows = _tasks.GetAll()
            .Select(t => new TaskListRow(t.Id, t.Title, t.Priority, t.BoardColumnId, t.ParentTaskItemId))
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

        var defaultColumn = board.Columns.OrderBy(c => c.Position).FirstOrDefault();
        var defaultStatus = project.TaskStatusDefinitions.FirstOrDefault(s => s.IsDefault)
                            ?? project.TaskStatusDefinitions.OrderBy(s => s.Position).FirstOrDefault();
        if (defaultColumn is null || defaultStatus is null)
            return NotFound();

        SetNav(project.WorkspaceId, project.Id);

        var vm = new TaskCreateFormVm
        {
            ProjectId = projectId,
            BoardId = board.Id,
            BoardColumnId = defaultColumn.Id,
            TaskStatusDefinitionId = defaultStatus.Id,
            ParentTaskItemId = parentTaskItemId
        };
        ViewBag.Project = project;
        ViewBag.Board = board;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TaskCreateFormVm model)
    {
        var project = _projects.GetById(model.ProjectId);
        if (project is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            var board = project.Boards.FirstOrDefault(b => b.Id == model.BoardId)
                        ?? project.Boards.OrderBy(b => b.Position).First();
            ViewBag.Project = project;
            ViewBag.Board = board;
            return View(model);
        }

        var req = new CreateTaskRequest(
            model.BoardColumnId,
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
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = task.Id });
    }

    public IActionResult Details(Guid id)
    {
        var entity = _tasks.GetById(id);
        var project = entity?.BoardColumn?.Board?.Project;
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

        var projectId = entity.BoardColumn?.Board?.ProjectId;
        if (!_tasks.TryDelete(id))
            return NotFound();

        if (projectId is { } pid)
            return RedirectToAction(nameof(ProjectsController.Details), "Projects", new { id = pid });

        return RedirectToAction(nameof(Index));
    }
}
