using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class UsersController : BaseController
{
    private readonly IUserRepository _users;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public UsersController(IUserRepository users, IBreadcrumbTrailBuilder breadcrumbs)
    {
        _users = users;
        _breadcrumbs = breadcrumbs;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        var rows = users
            .Select(u => new UserListRow(u.Id, u.FullName, u.Email, u.IsActive))
            .ToList();
        return View(rows);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _users.GetByIdAsync(id, ct);
        return ViewDetails(entity, _breadcrumbs.ForUser);
    }
}
