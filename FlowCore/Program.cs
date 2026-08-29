using System.Globalization;
using FlowCore.Configuration;
using FlowCore.Data;
using FlowCore.Observability;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// The Playwright process runs outside the interactive Windows profile that owns
// local DPAPI keys. Keep its cookie encryption keys in a writable temporary
// location, without changing the application's normal Development/Production setup.
if (string.Equals(Environment.GetEnvironmentVariable("FLOWCORE_E2E"), "true", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "flowcore-playwright-keys")))
        .SetApplicationName("FlowCore.Playwright");
}

// Keep the .env names simple; IConfiguration uses double underscores for nesting,
// so read these flat environment variables directly.
var betterStackSourceToken = Environment.GetEnvironmentVariable("BETTER_STACK_SOURCE_TOKEN");
var betterStackIngestingHost = Environment.GetEnvironmentVariable("BETTER_STACK_INGESTING_HOST");
var isBetterStackConfigured = !string.IsNullOrWhiteSpace(betterStackSourceToken)
                              && !string.IsNullOrWhiteSpace(betterStackIngestingHost);

builder.Logging.ClearProviders();
if (isBetterStackConfigured)
{
    builder.Logging.AddJsonConsole();
    builder.Logging.AddProvider(new BetterStackLoggerProvider(betterStackSourceToken!, betterStackIngestingHost!));
}
else
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
}

builder.Services.AddFlowCoreServices(builder.Configuration);

var app = builder.Build();

if (isBetterStackConfigured)
{
    app.Logger.LogInformation("Better Stack direct logging enabled. {IngestingHost}", betterStackIngestingHost);
}

await app.Services.InitializeDatabaseAsync(app.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    // API clients must retain their actual HTTP error response (for example 401),
    // rather than receiving the MVC status page as an HTML 200 response.
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/api"),
        branch => branch.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}"));
}

app.UseForwardedHeaders();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

var supportedCultures = new[]
{
    new CultureInfo("hr-HR"),
    new CultureInfo("en-US")
};
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("hr-HR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static void LoadDotEnv()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null)
    {
        var envPath = Path.Combine(directory.FullName, ".env");
        if (File.Exists(envPath))
        {
            foreach (var rawLine in File.ReadAllLines(envPath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim().Trim('"');
                if (string.IsNullOrEmpty(key) || Environment.GetEnvironmentVariable(key) is not null)
                    continue;

                Environment.SetEnvironmentVariable(key, value);
            }

            return;
        }

        directory = directory.Parent;
    }
}

public partial class Program;
