using FlowCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers;

public class CommentsController : BaseController
{
    private readonly ICommentRepository _comments;
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;
    private readonly IWorkspaceRepository _workspaces;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly FlowCoreDbContext _db;

    public CommentsController(
        ICommentRepository comments,
        ITaskRepository tasks,
        IUserRepository users,
        IBreadcrumbTrailBuilder breadcrumbs,
        IWorkspaceRepository workspaces,
        IAuthorizationService authz,
        ICurrentUserAccessor currentUser,
        FlowCoreDbContext db)
    {
        _comments = comments;
        _tasks = tasks;
        _users = users;
        _breadcrumbs = breadcrumbs;
        _workspaces = workspaces;
        _authz = authz;
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userWorkspaceIds = await _db.WorkspaceMembers
            .Where(m => m.UserId == _currentUser.UserId)
            .Select(m => m.WorkspaceId)
            .ToHashSetAsync(ct);

        var rows = await _db.Comments
            .AsNoTracking()
            .Where(c => userWorkspaceIds.Contains(c.TaskItem!.Board!.Project!.WorkspaceId))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentListRow(
                c.Id,
                c.TaskItemId,
                c.Author != null ? c.Author.FullName : "(unknown)",
                c.Body.Length > 80 ? c.Body.Substring(0, 80) + "…" : c.Body,
                c.CreatedAt))
            .ToListAsync(ct);

        return View(rows);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _comments.GetByIdAsync(id, ct);
        if (entity is null)
            return NotFound();

        var task = await _tasks.GetByIdAsync(entity.TaskItemId, ct);
        var project = task?.Board?.Project;
        if (project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
        }

        ViewBag.Breadcrumbs = _breadcrumbs.ForComment(entity, task?.Title ?? "(task)");
        return View(entity);
    }

    [HttpGet("/comments/{id:guid}/edit", Name = "comment-edit-form")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var entity = await _comments.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        var task = await _tasks.GetByIdAsync(entity.TaskItemId, ct);
        var project = task?.Board?.Project;
        if (project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
        }

        ViewBag.TaskTitle = entity.TaskItem?.Title;
        ViewBag.TaskId = entity.TaskItemId;
        return View(new CommentFormVm { Body = entity.Body });
    }

    [HttpPost("/comments/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CommentFormVm model, CancellationToken ct)
    {
        var entity = await _comments.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        var task = await _tasks.GetByIdAsync(entity.TaskItemId, ct);
        var project = task?.Board?.Project;
        if (project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
        }

        if (!ModelState.IsValid)
        {
            ViewBag.TaskTitle = entity.TaskItem?.Title;
            ViewBag.TaskId = entity.TaskItemId;
            return View(model);
        }

        var updated = await _comments.UpdateBodyAsync(id, model.Body.Trim(), ct);
        if (updated is null) return NotFound();

        return RedirectToAction("Details", "Tasks", new { id = entity.TaskItemId });
    }

    [HttpPost("/comments/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _comments.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        var task = await _tasks.GetByIdAsync(entity.TaskItemId, ct);
        var project = task?.Board?.Project;
        if (project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
        }

        var taskId = entity.TaskItemId;
        if (!await _comments.TryDeleteAsync(id, ct)) return NotFound();

        return RedirectToAction("Details", "Tasks", new { id = taskId });
    }
}
