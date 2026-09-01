using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FlowCore.Data;

public static class DemoSeedSettings
{
    private const string DevelopmentFallbackPassword = "Admin6060!";

    public static bool IsSeedingEnabled(IConfiguration configuration, IHostEnvironment environment)
    {
        return environment.IsDevelopment() || configuration.GetValue<bool>("Seed:Enabled");
    }

    public static string ResolveSharedPassword(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredPassword = configuration["Seed:SharedPassword"];
        if (!string.IsNullOrWhiteSpace(configuredPassword))
        {
            return configuredPassword;
        }

        if (environment.IsDevelopment())
        {
            return DevelopmentFallbackPassword;
        }

        throw new InvalidOperationException(
            "Seeded demo data requires a shared password in production. Set Seed__SharedPassword.");
    }
}
