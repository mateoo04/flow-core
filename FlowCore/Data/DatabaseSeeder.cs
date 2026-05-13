using FlowCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Data;

public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowCoreDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        if (await db.Workspaces.AnyAsync(ct))
            return;

        var graph = DemoDataBuilder.CreateSampleGraph(hasher);

        db.Users.AddRange(graph.Users);
        db.Tags.AddRange(graph.Tags);
        db.Workspaces.AddRange(graph.Workspaces);
        db.WorkspaceMembers.AddRange(graph.WorkspaceMembers);

        await db.SaveChangesAsync(ct);
    }
}
