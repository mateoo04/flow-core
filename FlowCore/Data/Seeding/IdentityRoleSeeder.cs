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

        var users = userManager.Users.ToList();
        foreach (var user in users)
        {
            if (!await userManager.IsInRoleAsync(user, AppRoles.User))
                await userManager.AddToRoleAsync(user, AppRoles.User);

            if (user.Id != DemoSeedIds.UserAlex
                && await userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                await userManager.RemoveFromRoleAsync(user, AppRoles.Admin);
            }
        }

        var alex = await userManager.FindByIdAsync(DemoSeedIds.UserAlex.ToString());
        if (alex is not null && !await userManager.IsInRoleAsync(alex, AppRoles.Admin))
            await userManager.AddToRoleAsync(alex, AppRoles.Admin);
    }
}
