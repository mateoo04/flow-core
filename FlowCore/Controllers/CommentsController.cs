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
}
