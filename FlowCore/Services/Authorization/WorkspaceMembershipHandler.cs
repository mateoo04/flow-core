using System.Security.Claims;
using FlowCore.Models;
using FlowCore.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace FlowCore.Services.Authorization;

public sealed class WorkspaceMembershipHandler
    : AuthorizationHandler<WorkspaceMembershipRequirement, Workspace>
{
    private readonly IWorkspaceRepository _workspaces;

    public WorkspaceMembershipHandler(IWorkspaceRepository workspaces) => _workspaces = workspaces;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx,
        WorkspaceMembershipRequirement requirement,
        Workspace workspace)
    {
        var rawId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (rawId is null || !Guid.TryParse(rawId, out var userId)) return;

        var membership = await _workspaces.GetMembershipAsync(workspace.Id, userId);
        if (membership is null) return;

        var rolesSufficient = membership.Role >= requirement.MinimumRole;

        if (rolesSufficient)
            ctx.Succeed(requirement);
    }
}
