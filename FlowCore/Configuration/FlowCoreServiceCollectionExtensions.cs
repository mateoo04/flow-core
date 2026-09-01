using System.Security.Claims;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Repositories;
using FlowCore.Repositories.EntityFramework;
using FlowCore.Services;
using FlowCore.Services.Attachments;
using FlowCore.Services.Ai;
using FlowCore.Services.Authorization;
using FlowCore.Services.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Configuration;

public static class FlowCoreServiceCollectionExtensions
{
    public static IServiceCollection AddFlowCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews(options =>
        {
            var requireAuth = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.Filters.Add(new AuthorizeFilter(requireAuth));
        });

        services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });

        var connectionString = PostgresConnectionStringResolver.ResolveFromConfiguration(configuration);
        services.AddDbContext<FlowCoreDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<FlowCoreDbContext>()
            .AddClaimsPrincipalFactory<FlowCoreUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        ConfigureGoogleAuthentication(services, configuration);
        ConfigureApplicationCookie(services);
        ConfigureRateLimiting(services);
        ConfigureAuthorization(services);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddHttpContextAccessor();
        services.AddHttpClient<IAiTaskExtractionService, OpenAiTaskExtractionService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IWorkspaceRepository, EfWorkspaceRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<ITaskRepository, EfTaskRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<ITagRepository, EfTagRepository>();
        services.AddScoped<IBoardRepository, EfBoardRepository>();
        services.AddScoped<ICommentRepository, EfCommentRepository>();
        services.AddScoped<IStatusRepository, EfStatusRepository>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IDemoDataResetService, DemoDataResetService>();
        services.AddScoped<IAuthorizationHandler, WorkspaceMembershipHandler>();
        services.AddScoped<IAuthorizationHandler, DemoUserHandler>();
        services.AddSingleton<IBreadcrumbTrailBuilder, BreadcrumbTrailBuilder>();
        services.AddSingleton<UiText>();
        services.Configure<AttachmentOptions>(configuration.GetSection(AttachmentOptions.SectionName));
        services.AddScoped<IAttachmentStorage, LocalDiskAttachmentStorage>();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }

    private static void ConfigureGoogleAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return;

        services.AddAuthentication().AddGoogle(options =>
        {
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.Events.OnCreatingTicket = context =>
            {
                if (context.Identity is not null
                    && context.User.TryGetProperty("email_verified", out var verified)
                    && verified.GetBoolean())
                {
                    context.Identity.AddClaim(new Claim("email_verified", "true"));
                }

                return Task.CompletedTask;
            };
        });
    }

    private static void ConfigureApplicationCookie(IServiceCollection services)
    {
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/account/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });
    }

    private static void ConfigureRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("auth", limiter =>
            {
                limiter.PermitLimit = 5;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }

    private static void ConfigureAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("WorkspaceMember", policy =>
                policy.Requirements.Add(new WorkspaceMembershipRequirement(WorkspaceRole.Member)));
            options.AddPolicy("WorkspaceOwner", policy =>
                policy.Requirements.Add(new WorkspaceMembershipRequirement(WorkspaceRole.Owner)));
            options.AddPolicy("DemoUser", policy => policy.Requirements.Add(new DemoUserRequirement()));
        });
    }
}
