using System.Net;
using System.Net.Http.Json;
using FlowCore.Models.Dtos;
using FlowCore.Tests.Infrastructure;
using Xunit;

namespace FlowCore.Tests.Api;

public class StatusesApiTests : IClassFixture<FlowCoreApiFactory>
{
    private readonly FlowCoreApiFactory _factory;

    public StatusesApiTests(FlowCoreApiFactory factory) => _factory = factory;

    private Task<FlowCore.Models.TaskStatusDefinition> SeedStatusAsync() =>
        _factory.WithDbContextAsync(async db =>
            await TestDataSeeder.CreateStatusAsync(db, await TestDataSeeder.CreateWorkspaceAsync(db)));

    [Fact]
    public async Task GetAll_ReturnsOkAndSeededStatus()
    {
        var status = await SeedStatusAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/statuses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<StatusDto>>();
        Assert.NotNull(items);
        Assert.Contains(items!, s => s.Id == status.Id);
    }

    [Fact]
    public async Task GetById_ReturnsStatus_WhenExists()
    {
        var status = await SeedStatusAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/statuses/{status.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<StatusDto>();
        Assert.Equal(status.Id, dto!.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/statuses/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesStatus_Returns201()
    {
        var workspace = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateWorkspaceAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new StatusCreateDto { WorkspaceId = workspace.Id, Name = "Doing", ColorHex = "#abcdef" };

        var response = await client.PostAsJsonAsync("/api/statuses", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<StatusDto>();
        Assert.Equal("Doing", dto!.Name);
        Assert.True(await _factory.WithDbContextAsync(db => DbAssert.StatusExistsAsync(db, dto.Id)));
    }

    [Fact]
    public async Task Post_Returns400_WhenInvalidColor()
    {
        var workspace = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateWorkspaceAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new StatusCreateDto { WorkspaceId = workspace.Id, Name = "Doing", ColorHex = "not-a-color" };

        var response = await client.PostAsJsonAsync("/api/statuses", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenWorkspaceMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new StatusCreateDto { WorkspaceId = Guid.NewGuid(), Name = "Doing", ColorHex = "#abcdef" };

        var response = await client.PostAsJsonAsync("/api/statuses", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesStatus_WhenExists()
    {
        var status = await SeedStatusAsync();
        var client = _factory.CreateAuthenticatedClient();
        var body = new StatusUpdateDto { Name = "Done", ColorHex = "#000000", Position = 2, IsDoneState = true };

        var response = await client.PutAsJsonAsync($"/api/statuses/{status.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<StatusDto>();
        Assert.Equal("Done", dto!.Name);
        Assert.True(dto.IsDoneState);
    }

    [Fact]
    public async Task Put_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new StatusUpdateDto { Name = "Done", ColorHex = "#000000" };
        var response = await client.PutAsJsonAsync($"/api/statuses/{Guid.NewGuid()}", body);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesStatus_WhenExists()
    {
        var status = await SeedStatusAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/statuses/{status.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await _factory.WithDbContextAsync(db => DbAssert.StatusExistsAsync(db, status.Id)));
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/statuses/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Returns401_WhenAnonymous()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/statuses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
