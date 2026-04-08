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

    public IActionResult Index()
    {
        var userMap = _users.GetAll().ToDictionary(u => u.Id);
        var rows = _comments.GetAll()
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

    public IActionResult Details(Guid id)
    {
        var entity = _comments.GetById(id);
        return ViewDetails(entity, e =>
        {
            var task = _tasks.GetById(e.TaskItemId);
            return _breadcrumbs.ForComment(e, task?.Title ?? "(task)");
        });
    }
}
