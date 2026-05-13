using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class WorkspacesController : BaseController
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IAuthorizationService _authz;
    private readonly IUserRepository _users;

    public WorkspacesController(
        IWorkspaceRepository workspaces,
        IBreadcrumbTrailBuilder breadcrumbs,
        ICurrentUserAccessor currentUser,
        IAuthorizationService authz,
        IUserRepository users)
    {
        _workspaces = workspaces;
        _breadcrumbs = breadcrumbs;
        _currentUser = currentUser;
        _authz = authz;
        _users = users;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var workspaces = await _workspaces.GetForUserAsync(_currentUser.UserId, ct);
        var rows = workspaces
            .Select(w => new WorkspaceListRow(w.Id, w.Name, w.Visibility, w.Projects.Count))
            .ToList();
        return View(rows);
    }

    [HttpGet("/workspaces/{id:guid}", Name = "workspace-details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (await EnsureWorkspaceMemberAsync(id, _workspaces, _authz, ct) is { } deny) return deny;

        var entity = await _workspaces.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        SetNav(entity.Id);

        var members = await _workspaces.GetMembersAsync(id, ct);
        ViewBag.Members = members
            .Select(m => new WorkspaceMemberRow(
                m.UserId, m.User.FullName, m.User.Email ?? "", m.Role, m.JoinedAt))
            .ToList();
        ViewBag.IsCurrentUserOwner = members.Any(m =>
            m.UserId == _currentUser.UserId && m.Role == WorkspaceRole.Owner);

        return ViewDetails(entity, _breadcrumbs.ForWorkspace);
    }

    [HttpGet("/workspaces/create", Name = "workspace-create-form")]
    public IActionResult Create() => View(new WorkspaceFormVm());

    [HttpPost("/workspaces/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkspaceFormVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var creatorId = _currentUser.UserId;
        var ws = await _workspaces.AddAsync(new Workspace
        {
            Id = Guid.NewGuid(),
            Name = model.Name.Trim(),
            Description = model.Description?.Trim() ?? "",
            Visibility = model.Visibility,
            CreatedAt = DateTime.UtcNow
        }, creatorId, ct);

        return RedirectToAction(nameof(Details), new { id = ws.Id });
    }

    [HttpGet("/workspaces/{id:guid}/edit", Name = "workspace-edit-form")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (await EnsureWorkspaceOwnerAsync(id, _workspaces, _authz, ct) is { } deny) return deny;

        var entity = await _workspaces.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        SetNav(entity.Id);
        return View(new WorkspaceFormVm
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Visibility = entity.Visibility
        });
    }

    [HttpPost("/workspaces/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, WorkspaceFormVm model, CancellationToken ct)
    {
        if (await EnsureWorkspaceOwnerAsync(id, _workspaces, _authz, ct) is { } deny) return deny;

        model.Id = id;
        if (!ModelState.IsValid)
            return View(model);

        var updated = await _workspaces.UpdateAsync(
            id,
            model.Name.Trim(),
            model.Description?.Trim() ?? "",
            model.Visibility,
            ct);
        if (updated is null) return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("/workspaces/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (await EnsureWorkspaceOwnerAsync(id, _workspaces, _authz, ct) is { } deny) return deny;

        if (await _workspaces.HasProjectsAsync(id, ct))
        {
            TempData["WorkspaceError"] = "Move or delete the projects in this workspace before deleting it.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!await _workspaces.TryDeleteAsync(id, ct))
            return NotFound();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/workspaces/{id:guid}/members")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(Guid id, AddWorkspaceMemberVm vm, CancellationToken ct)
    {
        if (await EnsureWorkspaceOwnerAsync(id, _workspaces, _authz, ct) is { } deny) return deny;

        if (!ModelState.IsValid)
        {
            TempData["MemberError"] = "Enter a valid email.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var target = await _users.FindByEmailAsync(vm.Email.Trim(), ct);
        if (target is null)
        {
            TempData["MemberError"] = "No user is registered with that email. Ask them to sign up first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var added = await _workspaces.AddMemberAsync(id, target.Id, WorkspaceRole.Member, ct);
        if (added is null)
        {
            TempData["MemberError"] = $"{target.FullName} is already a member.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["MemberInfo"] = $"Added {target.FullName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("/workspaces/{id:guid}/members/{userId:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        if (await EnsureWorkspaceOwnerAsync(id, _workspaces, _authz, ct) is { } deny) return deny;

        var members = await _workspaces.GetMembersAsync(id, ct);
        var target = members.FirstOrDefault(m => m.UserId == userId);
        if (target is null)
        {
            TempData["MemberError"] = "Member not found.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ownerCount = members.Count(m => m.Role == WorkspaceRole.Owner);
        if (target.Role == WorkspaceRole.Owner && ownerCount <= 1)
        {
            TempData["MemberError"] = "Transfer ownership before removing the sole owner.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await _workspaces.RemoveMemberAsync(id, userId, ct);
        TempData["MemberInfo"] = $"Removed {target.User.FullName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("/workspaces/{id:guid}/transfer-ownership")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferOwnership(Guid id, TransferOwnershipVm vm, CancellationToken ct)
    {
        if (await EnsureWorkspaceOwnerAsync(id, _workspaces, _authz, ct) is { } deny) return deny;

        if (!ModelState.IsValid)
        {
            TempData["MemberError"] = "Pick a member to transfer ownership to.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ok = await _workspaces.TransferOwnershipAsync(id, vm.NewOwnerUserId, ct);
        if (!ok)
        {
            TempData["MemberError"] = "Could not transfer ownership.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["MemberInfo"] = "Ownership transferred.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
