using System.Diagnostics;
using FlowCore.Common;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using FlowCore.Services.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class HomeController : BaseController
{
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly ITaskService _taskService;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IWorkspaceRepository _workspaces;
    private readonly IAuthorizationService _authz;

    public HomeController(
        ITaskRepository tasks,
        IUserRepository users,
        ITaskService taskService,
        ICurrentUserAccessor currentUser,
        IWorkspaceRepository workspaces,
        IAuthorizationService authz)
    {
        _tasks = tasks;
        _users = users;
        _taskService = taskService;
        _currentUser = currentUser;
        _workspaces = workspaces;
        _authz = authz;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true)
            return View("Landing");

        var currentUserId = _currentUser.UserId;
        var user = await _users.GetByIdAsync(currentUserId, ct);

        var tasks = await _tasks.GetAssignedToUserAsync(currentUserId, ct);
        var today = DateTime.UtcNow.Date;

        var groups = tasks
            .GroupBy(t => t.TaskStatusDefinition?.Name ?? "Unknown")
            .Select(g =>
            {
                var sortKey = g.Min(t => t.TaskStatusDefinition?.Position ?? 999);
                var color = g.Select(t => t.TaskStatusDefinition?.ColorHex).FirstOrDefault(c => !string.IsNullOrEmpty(c));
                var cards = g
                    .Select(t =>
                    {
                        var project = t.Board?.Project;
                        var due = t.DueDate;
                        var dueLabel = due is { } d ? d.ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture) : null;
                        var isOverdue = due is { } d2 && d2.Date < today;
                        var subtasks = t.Subtasks;
                        return new MyTaskCardVm
                        {
                            TaskId = t.Id,
                            Title = t.Title,
                            ProjectId = project?.Id ?? Guid.Empty,
                            ProjectName = project?.Name ?? "Project",
                            Assignees = TaskAssigneeStackBuilder.FromTask(t),
                            DueDateLabel = dueLabel,
                            IsOverdue = isOverdue,
                            SubtaskTotal = subtasks?.Count ?? 0,
                            SubtaskDone = subtasks?.Count(s => s.TaskStatusDefinition?.IsDoneState == true) ?? 0
                        };
                    })
                    .ToList();

                return new StatusTaskGroupVm
                {
                    StatusName = g.Key,
                    StatusColorHex = color,
                    SortKey = sortKey,
                    Tasks = cards
                };
            })
            .OrderBy(x => x.SortKey)
            .ThenBy(x => x.StatusName)
            .ToList();

        var vm = new MyWorkViewModel
        {
            CurrentUserDisplayName = user?.FullName ?? "You",
            StatusGroups = groups
        };
        return View(vm);
    }

    [HttpPost("/home/tasks/{id:guid}/move")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(
        Guid id,
        [FromBody] MoveOnHomeRequest body,
        CancellationToken ct)
    {
        if (body is null) return BadRequest();

        var task = await _tasks.GetByIdAsync(id, ct);
        if (task is null) return NotFound();
        var workspaceId = task.Board!.Project!.WorkspaceId;
        if (await EnsureWorkspaceMemberAsync(workspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        var currentUserId = _currentUser.UserId;
        var result = await _taskService.MoveOnHomeAsync(
            currentUserId, id, body.StatusName, body.Position, ct);

        if (result.IsSuccess) return NoContent();

        return result.Error!.Value.Kind switch
        {
            ErrorKind.NotFound => NotFound(),
            ErrorKind.Conflict => Conflict(result.Error.Value.Message),
            _ => BadRequest(result.Error.Value.Message)
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int code)
    {
        Response.StatusCode = code;

        if (code == StatusCodes.Status404NotFound)
        {
            return View("NotFound");
        }

        return Error();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
