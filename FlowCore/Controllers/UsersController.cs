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

    [HttpGet("/users/create", Name = "user-create-form")]
    public IActionResult Create() => View(new UserFormVm());

    [HttpPost("/users/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormVm model, CancellationToken ct)
    {
        await ValidateAsync(model, excludeId: null, ct);
        if (!ModelState.IsValid) return View(model);

        var user = await _users.AddAsync(new Models.User
        {
            Id = Guid.NewGuid(),
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            IsActive = model.IsActive,
            JoinedAt = DateTime.UtcNow
        }, ct);

        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    [HttpGet("/users/{id:guid}/edit", Name = "user-edit-form")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var entity = await _users.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        return View(new UserFormVm
        {
            Id = entity.Id,
            FullName = entity.FullName,
            Email = entity.Email,
            IsActive = entity.IsActive
        });
    }

    [HttpPost("/users/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UserFormVm model, CancellationToken ct)
    {
        model.Id = id;
        await ValidateAsync(model, excludeId: id, ct);
        if (!ModelState.IsValid) return View(model);

        var updated = await _users.UpdateAsync(id, model.FullName.Trim(), model.Email.Trim(), model.IsActive, ct);
        if (updated is null) return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    // Soft delete: deactivates the user but preserves history (TaskAssignments, Comments, OwnedWorkspaces).
    [HttpPost("/users/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var user = await _users.DeactivateAsync(id, ct);
        if (user is null) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(UserFormVm model, Guid? excludeId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(model.Email)
            && await _users.EmailExistsAsync(model.Email, excludeId, ct))
            ModelState.AddModelError(nameof(UserFormVm.Email), "A user with this email already exists.");
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
