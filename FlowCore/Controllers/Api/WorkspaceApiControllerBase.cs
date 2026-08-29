using System.Security.Claims;
using FlowCore.Data;
using FlowCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers.Api;

/// <summary>Shared identity and workspace-membership checks for workspace-scoped API resources.</summary>
public abstract class WorkspaceApiControllerBase(FlowCoreDbContext db) : ControllerBase
{
    protected FlowCoreDbContext Db { get; } = db;

    protected Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected async Task<bool> CanAccessWorkspaceAsync(Guid workspaceId, CancellationToken ct)
    {
        if (User.IsInRole(AppRoles.Admin))
            return true;

        if (CurrentUserId() is not { } userId)
            return false;

        return await Db.WorkspaceMembers.AnyAsync(
            member => member.WorkspaceId == workspaceId && member.UserId == userId,
            ct);
    }
}
