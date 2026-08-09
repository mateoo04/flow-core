using System.Globalization;
using System.Threading.RateLimiting;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Repositories;
using FlowCore.Repositories.EntityFramework;
using FlowCore.Services;
using FlowCore.Services.Attachments;
using FlowCore.Services.Authorization;
using FlowCore.Services.Domain;
using FlowCore.Validation;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(opts =>
{
    var requireAuth = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    opts.Filters.Add(new AuthorizeFilter(requireAuth));
});

builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

builder.Services.Configure<RouteOptions>(opts =>
{
    opts.LowercaseUrls = true;
    opts.LowercaseQueryStrings = true;
});

var configuredDbConnection = PostgresConnectionStringResolver.ResolveFromConfiguration(builder.Configuration);

builder.Services.AddDbContext<FlowCoreDbContext>(options =>
    options.UseNpgsql(
        configuredDbConnection,
        npg => npg.EnableRetryOnFailure()));

builder.Services
    .AddIdentity<User, IdentityRole<Guid>>(opts =>
    {
        opts.User.RequireUniqueEmail = true;
        // Identity default password policy: 8+ chars, upper/lower/digit/symbol.
        opts.SignIn.RequireConfirmedAccount = false;
        opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        opts.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<FlowCoreDbContext>()
    .AddClaimsPrincipalFactory<FlowCoreUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.Events.OnCreatingTicket = ctx =>
        {
            if (ctx.Identity is not null
                && ctx.User.TryGetProperty("email_verified", out var verified)
                && verified.GetBoolean())
            {
                ctx.Identity.AddClaim(new Claim("email_verified", "true"));
            }

            return Task.CompletedTask;
        };
    });
}

builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = "/account/login";
    opts.AccessDeniedPath = "/account/access-denied";
    opts.ExpireTimeSpan = TimeSpan.FromDays(7);
    opts.SlidingExpiration = true;

    opts.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    opts.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("WorkspaceMember",
        p => p.Requirements.Add(new WorkspaceMembershipRequirement(WorkspaceRole.Member)));
    opts.AddPolicy("WorkspaceOwner",
        p => p.Requirements.Add(new WorkspaceMembershipRequirement(WorkspaceRole.Owner)));
    opts.AddPolicy("DemoUser",
        p => p.Requirements.Add(new DemoUserRequirement()));
});

builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opts.KnownIPNetworks.Clear();
    opts.KnownProxies.Clear();
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IWorkspaceRepository, EfWorkspaceRepository>();
builder.Services.AddScoped<IProjectRepository, EfProjectRepository>();
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<ITagRepository, EfTagRepository>();
builder.Services.AddScoped<IBoardRepository, EfBoardRepository>();
builder.Services.AddScoped<ICommentRepository, EfCommentRepository>();
builder.Services.AddScoped<IStatusRepository, EfStatusRepository>();

builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();

builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IDemoDataResetService, DemoDataResetService>();
builder.Services.AddScoped<IAuthorizationHandler, WorkspaceMembershipHandler>();
builder.Services.AddScoped<IAuthorizationHandler, DemoUserHandler>();

builder.Services.AddSingleton<IBreadcrumbTrailBuilder, BreadcrumbTrailBuilder>();
builder.Services.AddSingleton<UiText>();

builder.Services.Configure<AttachmentOptions>(
    builder.Configuration.GetSection(AttachmentOptions.SectionName));
builder.Services.AddScoped<IAttachmentStorage, LocalDiskAttachmentStorage>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(app.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");
}

app.UseForwardedHeaders();
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
