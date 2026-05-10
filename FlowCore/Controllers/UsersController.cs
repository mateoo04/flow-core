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

    [HttpGet("/users/autocomplete")]
    public async Task<IActionResult> Autocomplete(
        [FromQuery] string? q,
        [FromQuery(Name = "fieldName")] string? fieldName,
        [FromQuery(Name = "exclude")] Guid[]? exclude,
        CancellationToken ct)
    {
        var query = (q ?? string.Empty).Trim();
        var field = string.IsNullOrWhiteSpace(fieldName) ? "Ids" : fieldName;
        var excludeIds = (IReadOnlyCollection<Guid>?)exclude ?? Array.Empty<Guid>();

        IReadOnlyList<AutocompleteChipVm> items = Array.Empty<AutocompleteChipVm>();
        if (query.Length > 0)
        {
            var users = await _users.SearchActiveAsync(query, excludeIds, take: 10, ct);
            items = users.Select(u => new AutocompleteChipVm(
                new AutocompleteItem(
                    u.Id,
                    u.FullName,
                    u.Email,
                    UserDisplayHelper.GetInitials(u.FullName),
                    UserDisplayHelper.BackgroundColorForUser(u.Id)),
                field)).ToList();
        }

        return PartialView("_AutocompleteResultList", new AutocompleteResultListVm(items));
    }
}
