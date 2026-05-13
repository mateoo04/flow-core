using System.Globalization;
using System.Threading.RateLimiting;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Repositories;
using FlowCore.Repositories.EntityFramework;
using FlowCore.Services;
using FlowCore.Services.Authorization;
using FlowCore.Services.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

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

builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = "/account/login";
    opts.AccessDeniedPath = "/account/access-denied";
    opts.ExpireTimeSpan = TimeSpan.FromDays(7);
    opts.SlidingExpiration = true;
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

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(app.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");

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

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
