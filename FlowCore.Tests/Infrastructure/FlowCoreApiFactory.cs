using FlowCore.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowCore.Tests.Infrastructure;

public sealed class FlowCoreApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"FlowCoreTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FlowCoreDbContext"] = "Host=test;Database=test;Username=test;Password=test",
                ["Database:AutoMigrate"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<FlowCoreDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                (d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") ?? false)).ToList();
            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<FlowCoreDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuth.Scheme;
                    options.DefaultChallengeScheme = TestAuth.Scheme;
                    options.DefaultScheme = TestAuth.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuth.Scheme, _ => { });

            services.RemoveAll(typeof(FlowCore.Services.Attachments.IAttachmentStorage));
            services.AddSingleton<FlowCore.Services.Attachments.IAttachmentStorage, FakeAttachmentStorage>();
            services.AddSingleton<FakeAttachmentStorage>(sp =>
                (FakeAttachmentStorage)sp.GetRequiredService<FlowCore.Services.Attachments.IAttachmentStorage>());

            services.RemoveAll(typeof(Microsoft.AspNetCore.Antiforgery.IAntiforgery));
            services.AddSingleton<Microsoft.AspNetCore.Antiforgery.IAntiforgery, NoopAntiforgery>();
        });
    }

    public HttpClient CreateAuthenticatedClient() => CreateClient();

    public HttpClient CreateAnonymousClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuth.AnonymousHeader, "true");
        return client;
    }

    public HttpClient CreateClientInRole(string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuth.RoleHeader, role);
        return client;
    }

    public async Task<T> WithDbContextAsync<T>(Func<FlowCoreDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowCoreDbContext>();
        return await action(db);
    }
}
