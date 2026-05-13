using FlowCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FlowCore.Data;

public static class DatabaseSeeder
{
    public static async Task SeedDemoDataAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowCoreDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        if (!DemoSeedSettings.IsSeedingEnabled(configuration, environment))
            return;

        if (await db.Workspaces.AnyAsync(ct))
            return;

        var sharedPassword = DemoSeedSettings.ResolveSharedPassword(configuration, environment);
        var graph = DemoDataBuilder.CreateSampleGraph(hasher, sharedPassword);

        db.Users.AddRange(graph.Users);
        db.Tags.AddRange(graph.Tags);
        db.Workspaces.AddRange(graph.Workspaces);
        db.WorkspaceMembers.AddRange(graph.WorkspaceMembers);

        await db.SaveChangesAsync(ct);
    }
}
