using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class CommentsController : BaseController
{
    private readonly ICommentRepository _comments;
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public CommentsController(
        ICommentRepository comments,
        ITaskRepository tasks,
        IUserRepository users,
        IBreadcrumbTrailBuilder breadcrumbs)
    {
        _comments = comments;
        _tasks = tasks;
        _users = users;
        _breadcrumbs = breadcrumbs;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        var userMap = users.ToDictionary(u => u.Id);
        var comments = await _comments.GetAllAsync(ct);
        var rows = comments
            .Select(c =>
            {
                var author = userMap.TryGetValue(c.AuthorUserId, out var u) ? u.FullName : "(unknown)";
                var preview = c.Body.Length > 80 ? string.Concat(c.Body.AsSpan(0, 80), "…") : c.Body;
                return new CommentListRow(c.Id, c.TaskItemId, author, preview, c.CreatedAt);
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return View(rows);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _comments.GetByIdAsync(id, ct);
        if (entity is null)
            return NotFound();
        var task = await _tasks.GetByIdAsync(entity.TaskItemId, ct);
        ViewBag.Breadcrumbs = _breadcrumbs.ForComment(entity, task?.Title ?? "(task)");
        return View(entity);
    }

    // TODO: restrict to author once auth lands.
    [HttpGet("/comments/{id:guid}/edit", Name = "comment-edit-form")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var entity = await _comments.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        ViewBag.TaskTitle = entity.TaskItem?.Title;
        ViewBag.TaskId = entity.TaskItemId;
        return View(new CommentFormVm { Body = entity.Body });
    }

    // TODO: restrict to author once auth lands.
    [HttpPost("/comments/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CommentFormVm model, CancellationToken ct)
    {
        var entity = await _comments.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

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

    // TODO: restrict to author once auth lands.
    [HttpPost("/comments/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _comments.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        var taskId = entity.TaskItemId;
        if (!await _comments.TryDeleteAsync(id, ct)) return NotFound();

        return RedirectToAction("Details", "Tasks", new { id = taskId });
    }
}
