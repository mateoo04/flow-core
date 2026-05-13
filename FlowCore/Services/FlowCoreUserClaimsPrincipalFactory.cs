using System.Security.Claims;
using FlowCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FlowCore.Services;

public sealed class FlowCoreUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<User, IdentityRole<Guid>>
{
    public FlowCoreUserClaimsPrincipalFactory(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            var existing = identity.FindFirst(ClaimTypes.Name);
            if (existing is not null)
                identity.RemoveClaim(existing);
            identity.AddClaim(new Claim(ClaimTypes.Name, user.FullName));
        }
        return identity;
    }
}
