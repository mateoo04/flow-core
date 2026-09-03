using System.Net;
using System.Net.Http.Json;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using FlowCore.Tests.Infrastructure;
using Xunit;

namespace FlowCore.Tests.Api;

public class WorkspacesApiTests : IClassFixture<FlowCoreApiFactory>
{
    private readonly FlowCoreApiFactory _factory;

    public WorkspacesApiTests(FlowCoreApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAll_ReturnsOkAndSeededWorkspace()
    {
        var workspace = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateWorkspaceAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/workspaces");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<WorkspaceDto>>();
        Assert.NotNull(items);
        Assert.Contains(items!, w => w.Id == workspace.Id);
    }

    [Fact]
    public async Task GetById_ReturnsWorkspace_WhenExists()
    {
        var workspace = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateWorkspaceAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(dto);
        Assert.Equal(workspace.Id, dto!.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/workspaces/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesWorkspace_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new WorkspaceCreateDto { Name = "New WS", Description = "d" };

        var response = await client.PostAsJsonAsync("/api/workspaces", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(dto);
        Assert.Equal("New WS", dto!.Name);
        Assert.EndsWith($"/api/workspaces/{dto.Id}", response.Headers.Location!.ToString());
        Assert.True(await _factory.WithDbContextAsync(db => DbAssert.WorkspaceExistsAsync(db, dto.Id)));
    }

    [Fact]
    public async Task Post_Returns400_WhenInvalid()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new WorkspaceCreateDto { Name = "", Description = "d" };

        var response = await client.PostAsJsonAsync("/api/workspaces", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesWorkspace_WhenExists()
    {
        var workspace = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateWorkspaceAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new WorkspaceUpdateDto { Name = "Renamed", Description = "x" };

        var response = await client.PutAsJsonAsync($"/api/workspaces/{workspace.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.Equal("Renamed", dto!.Name);
    }

    [Fact]
    public async Task Put_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new WorkspaceUpdateDto { Name = "Renamed", Description = "x" };
        var response = await client.PutAsJsonAsync($"/api/workspaces/{Guid.NewGuid()}", body);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesWorkspace_WhenExists()
    {
        var workspace = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateWorkspaceAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await _factory.WithDbContextAsync(db => DbAssert.WorkspaceExistsAsync(db, workspace.Id)));
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/workspaces/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Returns401_WhenAnonymous()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/workspaces");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
