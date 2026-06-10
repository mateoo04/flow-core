using System.Net;
using FlowCore.Models;
using FlowCore.Tests.Infrastructure;
using Xunit;

namespace FlowCore.Tests.Auth;

public class AdminAuthorizationTests : IClassFixture<FlowCoreApiFactory>
{
    private readonly FlowCoreApiFactory _factory;

    public AdminAuthorizationTests(FlowCoreApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_Returns401_WhenAnonymous()
    {
        var response = await _factory.CreateAnonymousClient().GetAsync("/admin");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Returns403_ForNonAdmin()
    {
        var response = await _factory.CreateAuthenticatedClient().GetAsync("/admin");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Returns200_ForAdmin()
    {
        var response = await _factory.CreateClientInRole(AppRoles.Admin).GetAsync("/admin");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Landing_IsPublic_ForAnonymous()
    {
        var response = await _factory.CreateAnonymousClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Get started", html);
    }
}
