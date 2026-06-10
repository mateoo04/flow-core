using Microsoft.EntityFrameworkCore;

namespace FlowCore.Data;

public static class StartupDatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IConfiguration configuration)
    {
        await ApplyMigrationsAsync(services, configuration);
        await services.SeedDemoDataAsync();
        await services.SeedRolesAsync();
    }

    private static async Task ApplyMigrationsAsync(IServiceProvider services, IConfiguration configuration)
    {
        var autoMigrate = configuration.GetValue("Database:AutoMigrate", true);
        if (!autoMigrate)
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowCoreDbContext>();
        await db.Database.MigrateAsync();
    }
}
