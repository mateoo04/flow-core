using FlowCore.Common;
using FlowCore.Data;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using FlowCore.Services.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class TasksController : BaseController
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly ITaskService _taskService;
    private readonly ICommentService _commentService;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public TasksController(
        ITaskRepository tasks,
        IProjectRepository projects,
        ITaskService taskService,
        ICommentService commentService,
        IBreadcrumbTrailBuilder breadcrumbs)
    {
        _tasks = tasks;
        _projects = projects;
        _taskService = taskService;
        _commentService = commentService;
        _breadcrumbs = breadcrumbs;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var tasks = await _tasks.GetAllAsync(ct);
        var rows = tasks
            .Select(t => new TaskListRow(t.Id, t.Title, t.Priority, t.BoardId, t.ParentTaskItemId))
            .OrderBy(r => r.Title)
            .ToList();
        return View(rows);
    }

    [HttpGet("/projects/{projectId:guid}/tasks/new", Name = "task-create-form")]
    public async Task<IActionResult> Create(Guid projectId, Guid? boardId, Guid? parentTaskItemId, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project is null)
            return NotFound();

        var board = boardId is { } bid
            ? project.Boards.FirstOrDefault(b => b.Id == bid)
            : null;
        board ??= project.Boards.OrderBy(b => b.Position).FirstOrDefault(b => b.IsDefault);
        board ??= project.Boards.OrderBy(b => b.Position).FirstOrDefault();
        if (board is null)
            return NotFound();

        var workspace = project.Workspace;
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

    [HttpPost("/projects/{projectId:guid}/tasks")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid projectId, TaskCreateFormVm model, CancellationToken ct)
    {
        model.ProjectId = projectId;
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project is null)
            return NotFound();

        var workspace = project.Workspace;
        var statuses = workspace?.TaskStatusDefinitions.OrderBy(s => s.Position).ToList()
                       ?? new List<Models.TaskStatusDefinition>();

        if (!ModelState.IsValid)
            return RenderForm(project, model, statuses);

        var req = new CreateTaskRequest(
            model.BoardId,
            model.TaskStatusDefinitionId,
            model.Title,
            model.Description,
            model.Priority,
            model.StoryPoints,
            model.ParentTaskItemId,
            model.DueDate);

        var result = await _taskService.CreateAsync(req, ct);
        if (result.IsSuccess)
            return RedirectToAction(nameof(Details), new { id = result.Value!.Id });

        if (result.Error!.Value.Kind == ErrorKind.NotFound)
            return NotFound();

        ModelState.AddModelError(string.Empty, result.Error.Value.Message);
        return RenderForm(project, model, statuses);
    }

    [HttpGet("/tasks/{id:guid}", Name = "task-details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _tasks.GetByIdAsync(id, ct);
        var project = entity?.Board?.Project;
        if (project is not null)
            SetNav(project.WorkspaceId, project.Id);
        return ViewDetails(entity, _breadcrumbs.ForTask);
    }

    [HttpPost("/tasks/{id:guid}/comments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, CommentFormVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Details), new { id });

        var result = await _commentService.CreateAsync(id, DemoSeedIds.UserAlex, model.Body, ct);
        if (result.Error?.Kind == ErrorKind.NotFound)
            return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("/tasks/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _tasks.GetByIdAsync(id, ct);
        if (entity is null)
            return NotFound();

        var projectId = entity.Board?.ProjectId;
        if (!await _tasks.TryDeleteAsync(id, ct))
            return NotFound();

        if (projectId is { } pid)
            return RedirectToAction(nameof(ProjectsController.Details), "Projects", new { id = pid });

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/tasks/{id:guid}/move")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(
        Guid id,
        [FromBody] MoveTaskRequest body,
        CancellationToken ct)
    {
        if (body is null) return BadRequest();

        var result = await _taskService.MoveAsync(id, body.StatusId, body.Position, ct);
        if (result.IsSuccess) return NoContent();

        return result.Error!.Value.Kind switch
        {
            ErrorKind.NotFound => NotFound(),
            ErrorKind.Conflict => Conflict(result.Error.Value.Message),
            _ => BadRequest(result.Error.Value.Message)
        };
    }

    private IActionResult RenderForm(Models.Project project, TaskCreateFormVm model, IReadOnlyList<Models.TaskStatusDefinition> statuses)
    {
        var board = project.Boards.FirstOrDefault(b => b.Id == model.BoardId)
                    ?? project.Boards.OrderBy(b => b.Position).First();
        ViewBag.Project = project;
        ViewBag.Board = board;
        ViewBag.Statuses = statuses;
        return View(model);
    }
}
