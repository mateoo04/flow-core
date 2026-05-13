using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public abstract class BaseController : Controller
{
    protected void SetNav(Guid? workspaceId, Guid? projectId = null)
    {
        if (workspaceId is { } ws)
            ViewData["ActiveWorkspaceId"] = ws;
        if (projectId is { } p)
            ViewData["ActiveProjectId"] = p;
    }

    protected IActionResult ViewDetails<T>(T? entity, Func<T, IReadOnlyList<BreadcrumbItem>> breadcrumbTrail) where T : class
    {
        if (entity is null)
            return NotFound();
        ViewBag.Breadcrumbs = breadcrumbTrail(entity);
        return View(entity);
    }

    /// <summary>
    /// Loads the workspace and checks the current user is at least a Member.
    /// Returns NotFound if the workspace is missing, Forbid if the user isn't a member, or null if authorized.
    /// </summary>
    protected async Task<IActionResult?> EnsureWorkspaceMemberAsync(
        Guid workspaceId,
        IWorkspaceRepository workspaces,
        IAuthorizationService authz,
        CancellationToken ct = default)
        => await EnsureWorkspaceAccessAsync(workspaceId, "WorkspaceMember", workspaces, authz, ct);

    protected async Task<IActionResult?> EnsureWorkspaceOwnerAsync(
        Guid workspaceId,
        IWorkspaceRepository workspaces,
        IAuthorizationService authz,
        CancellationToken ct = default)
        => await EnsureWorkspaceAccessAsync(workspaceId, "WorkspaceOwner", workspaces, authz, ct);

    private async Task<IActionResult?> EnsureWorkspaceAccessAsync(
        Guid workspaceId,
        string policy,
        IWorkspaceRepository workspaces,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        var ws = await workspaces.GetByIdAsync(workspaceId, ct);
        if (ws is null) return NotFound();

        var result = await authz.AuthorizeAsync(User, ws, policy);
        return result.Succeeded ? null : Forbid();
    }
}
