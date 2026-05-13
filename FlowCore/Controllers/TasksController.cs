using FlowCore.Common;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using FlowCore.Services.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class TasksController : BaseController
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly ITaskService _taskService;
    private readonly ICommentService _commentService;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IAuthorizationService _authz;

    public TasksController(
        ITaskRepository tasks,
        IProjectRepository projects,
        IUserRepository users,
        ITaskService taskService,
        ICommentService commentService,
        IBreadcrumbTrailBuilder breadcrumbs,
        IWorkspaceRepository workspaces,
        ICurrentUserAccessor currentUser,
        IAuthorizationService authz)
    {
        _tasks = tasks;
        _projects = projects;
        _users = users;
        _taskService = taskService;
        _commentService = commentService;
        _breadcrumbs = breadcrumbs;
        _workspaces = workspaces;
        _currentUser = currentUser;
        _authz = authz;
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
        if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;

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
        if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;

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
            model.DueDate,
            model.AssigneeIds);

        var result = await _taskService.CreateAsync(req, ct);
        if (result.IsSuccess)
            return RedirectToAction(nameof(Details), new { id = result.Value!.Id });

        if (result.Error!.Value.Kind == ErrorKind.NotFound)
            return NotFound();

        ModelState.AddModelError(string.Empty, result.Error.Value.Message);
        return RenderForm(project, model, statuses);
    }

    [HttpGet("/tasks/{id:guid}/edit", Name = "task-edit-form")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var task = await _tasks.GetForEditAsync(id, ct);
        if (task is null) return NotFound();

        var project = task.Board?.Project;
        var workspace = project?.Workspace;
        if (project is null || workspace is null) return NotFound();
        if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        var statuses = workspace.TaskStatusDefinitions.OrderBy(s => s.Position).ToList();
        SetNav(project.WorkspaceId, project.Id);

        var vm = BuildEditVm(task);

        ViewBag.Project = project;
        ViewBag.Board = task.Board;
        ViewBag.Statuses = statuses;
        return View(vm);
    }

    [HttpPost("/tasks/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TaskEditFormVm model, CancellationToken ct)
    {
        model.Id = id;

        // Load task to derive workspace id for auth check
        var taskForAuth = await _tasks.GetForEditAsync(id, ct);
        if (taskForAuth is null) return NotFound();
        var projectForAuth = taskForAuth.Board?.Project;
        if (projectForAuth is null) return NotFound();
        if (await EnsureWorkspaceMemberAsync(projectForAuth.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        if (!ModelState.IsValid)
            return await RenderEditFormAsync(id, model, ct);

        var req = new UpdateTaskRequest(
            id,
            model.TaskStatusDefinitionId,
            model.Title,
            model.Description,
            model.Priority,
            model.StoryPoints,
            model.DueDate,
            model.AssigneeIds);

        var result = await _taskService.UpdateAsync(req, ct);
        if (result.IsSuccess)
            return RedirectToAction(nameof(Details), new { id });

        if (result.Error!.Value.Kind == ErrorKind.NotFound)
            return NotFound();

        ModelState.AddModelError(string.Empty, result.Error.Value.Message);
        return await RenderEditFormAsync(id, model, ct);
    }

    [HttpGet("/tasks/{id:guid}", Name = "task-details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _tasks.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        var project = entity.Board?.Project;
        if (project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
            SetNav(project.WorkspaceId, project.Id);
        }

        return ViewDetails(entity, _breadcrumbs.ForTask);
    }

    [HttpPost("/tasks/{id:guid}/comments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, CommentFormVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Details), new { id });

        var task = await _tasks.GetByIdAsync(id, ct);
        if (task is null) return NotFound();
        var project = task.Board?.Project;
        if (project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
        }

        var result = await _commentService.CreateAsync(id, _currentUser.UserId, model.Body, ct);
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

        var project = entity.Board?.Project;
        if (project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
        }

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

        var task = await _tasks.GetByIdAsync(id, ct);
        if (task is null) return NotFound();
        var workspaceId = task.Board?.Project?.WorkspaceId;
        if (workspaceId is null) return NotFound();
        if (await EnsureWorkspaceMemberAsync(workspaceId.Value, _workspaces, _authz, ct) is { } deny) return deny;

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

    private async Task<IActionResult> RenderEditFormAsync(Guid id, TaskEditFormVm model, CancellationToken ct)
    {
        var task = await _tasks.GetForEditAsync(id, ct);
        if (task is null) return NotFound();

        var project = task.Board?.Project;
        var workspace = project?.Workspace;
        if (project is null || workspace is null) return NotFound();

        var statuses = workspace.TaskStatusDefinitions.OrderBy(s => s.Position).ToList();
        SetNav(project.WorkspaceId, project.Id);

        var users = await _users.GetByIdsAsync(model.AssigneeIds, ct);
        model.SelectedAssignees = users
            .Select(u => new AutocompleteItem(
                u.Id,
                u.FullName,
                u.Email,
                UserDisplayHelper.GetInitials(u.FullName),
                UserDisplayHelper.BackgroundColorForUser(u.Id)))
            .ToList();

        ViewBag.Project = project;
        ViewBag.Board = task.Board;
        ViewBag.Statuses = statuses;
        return View(model);
    }

    private static TaskEditFormVm BuildEditVm(Models.TaskItem task)
    {
        var assignees = task.TaskAssignments
            .Where(a => a.User is not null)
            .Select(a => a.User!)
            .DistinctBy(u => u.Id)
            .OrderBy(u => u.FullName)
            .ToList();

        return new TaskEditFormVm
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            TaskStatusDefinitionId = task.TaskStatusDefinitionId,
            Priority = task.Priority,
            StoryPoints = task.StoryPoints,
            DueDate = task.DueDate,
            AssigneeIds = assignees.Select(u => u.Id).ToList(),
            SelectedAssignees = assignees
                .Select(u => new AutocompleteItem(
                    u.Id,
                    u.FullName,
                    u.Email,
                    UserDisplayHelper.GetInitials(u.FullName),
                    UserDisplayHelper.BackgroundColorForUser(u.Id)))
                .ToList()
        };
    }
}
