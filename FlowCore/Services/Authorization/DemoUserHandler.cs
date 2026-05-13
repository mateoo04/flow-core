using System.Security.Claims;
using FlowCore.Data;
using Microsoft.AspNetCore.Authorization;

namespace FlowCore.Services.Authorization;

public sealed class DemoUserHandler : AuthorizationHandler<DemoUserRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx,
        DemoUserRequirement requirement)
    {
        var email = ctx.User.FindFirstValue(ClaimTypes.Email);
        if (string.Equals(email, DemoSeedIds.UserDemoEmail, StringComparison.OrdinalIgnoreCase))
            ctx.Succeed(requirement);
        return Task.CompletedTask;
    }
}
