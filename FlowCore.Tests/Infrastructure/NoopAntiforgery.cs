using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace FlowCore.Tests.Infrastructure;

public sealed class NoopAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => Tokens();
    public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => Tokens();
    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
    public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
    public void SetCookieTokenAndHeader(HttpContext httpContext) { }

    private static AntiforgeryTokenSet Tokens() =>
        new("test", "test", "RequestVerificationToken", "RequestVerificationToken");
}
