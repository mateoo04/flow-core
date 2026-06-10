using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowCore.Tests.Infrastructure;

public static class TestAuth
{
    public const string Scheme = "Test";

    public const string AnonymousHeader = "X-Test-Anonymous";
    public const string RoleHeader = "X-Test-Role";

    public static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string Email = "test.user@flowcore.local";
    public const string FullName = "Test User";
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(TestAuth.AnonymousHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, TestAuth.UserId.ToString()),
            new(ClaimTypes.Name, TestAuth.Email),
            new(ClaimTypes.Email, TestAuth.Email)
        };

        if (Request.Headers.TryGetValue(TestAuth.RoleHeader, out var roleValues))
        {
            foreach (var role in roleValues)
                if (!string.IsNullOrEmpty(role))
                    claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, TestAuth.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestAuth.Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
