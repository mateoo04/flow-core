using Microsoft.EntityFrameworkCore;

namespace FlowCore.Data;

public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowCoreDbContext>();

        if (await db.Workspaces.AnyAsync(ct))
            return;

        var graph = DemoDataBuilder.CreateSampleGraph();

        db.Users.AddRange(graph.Users);
        db.Tags.AddRange(graph.Tags);
        db.Workspaces.AddRange(graph.Workspaces);

        await db.SaveChangesAsync(ct);
    }
}
