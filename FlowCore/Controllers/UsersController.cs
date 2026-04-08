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

    public IActionResult Index()
    {
        var rows = _users.GetAll()
            .Select(u => new UserListRow(u.Id, u.FullName, u.Email, u.IsActive))
            .ToList();
        return View(rows);
    }

    public IActionResult Details(Guid id)
    {
        var entity = _users.GetById(id);
        return ViewDetails(entity, _breadcrumbs.ForUser);
    }
}
