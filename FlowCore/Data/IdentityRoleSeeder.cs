using FlowCore.Models;
using Microsoft.AspNetCore.Identity;

namespace FlowCore.Data;

public static class IdentityRoleSeeder
{
    public static async Task SeedRolesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        foreach (var user in userManager.Users.ToList())
        {
            if (!await userManager.IsInRoleAsync(user, AppRoles.User))
                await userManager.AddToRoleAsync(user, AppRoles.User);
        }

        var demo = await userManager.FindByEmailAsync(DemoSeedIds.UserDemoEmail);
        if (demo is not null && !await userManager.IsInRoleAsync(demo, AppRoles.Admin))
            await userManager.AddToRoleAsync(demo, AppRoles.Admin);
    }
}
