using System.Net;
using System.Net.Http.Json;
using FlowCore.Models;
using FlowCore.Models.Dtos;
using FlowCore.Tests.Infrastructure;
using Xunit;

namespace FlowCore.Tests.Api;

public class ProjectsApiTests : IClassFixture<FlowCoreApiFactory>
{
    private readonly FlowCoreApiFactory _factory;

    public ProjectsApiTests(FlowCoreApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAll_ReturnsOkAndSeededProject()
    {
        var project = await _factory.WithDbContextAsync(async db =>
            await TestDataSeeder.CreateProjectAsync(db, await TestDataSeeder.CreateWorkspaceAsync(db)));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<ProjectDto>>();
        Assert.NotNull(items);
        Assert.Contains(items!, p => p.Id == project.Id);
    }

    [Fact]
    public async Task GetById_ReturnsProjectWithNestedWorkspace_WhenExists()
    {
        var (workspace, project) = await _factory.WithDbContextAsync(async db =>
        {
            var ws = await TestDataSeeder.CreateWorkspaceAsync(db);
            var p = await TestDataSeeder.CreateProjectAsync(db, ws);
            return (ws, p);
        });
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(dto);
        Assert.Equal(project.Id, dto!.Id);
        Assert.NotNull(dto.Workspace);
        Assert.Equal(workspace.Id, dto.Workspace!.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/projects/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesProject_Returns201()
    {
        var workspace = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateWorkspaceAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new ProjectCreateDto
        {
            WorkspaceId = workspace.Id,
            Name = "New Project",
            Description = "d",
            Status = ProjectStatus.Active,
            Priority = ProjectPriority.High
        };

        var response = await client.PostAsJsonAsync("/api/projects", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(dto);
        Assert.Equal("New Project", dto!.Name);
        Assert.True(await _factory.WithDbContextAsync(db => DbAssert.ProjectExistsAsync(db, dto.Id)));
    }

    [Fact]
    public async Task Post_Returns400_WhenInvalid()
    {
        var workspace = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateWorkspaceAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new ProjectCreateDto { WorkspaceId = workspace.Id, Name = "" };

        var response = await client.PostAsJsonAsync("/api/projects", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenWorkspaceMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new ProjectCreateDto { WorkspaceId = Guid.NewGuid(), Name = "Orphan" };

        var response = await client.PostAsJsonAsync("/api/projects", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesProject_WhenExists()
    {
        var project = await _factory.WithDbContextAsync(async db =>
            await TestDataSeeder.CreateProjectAsync(db, await TestDataSeeder.CreateWorkspaceAsync(db)));
        var client = _factory.CreateAuthenticatedClient();
        var body = new ProjectUpdateDto { Name = "Renamed", Description = "x", Status = ProjectStatus.Completed };

        var response = await client.PutAsJsonAsync($"/api/projects/{project.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.Equal("Renamed", dto!.Name);
        Assert.Equal(ProjectStatus.Completed, dto.Status);
    }

    [Fact]
    public async Task Put_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new ProjectUpdateDto { Name = "Renamed" };
        var response = await client.PutAsJsonAsync($"/api/projects/{Guid.NewGuid()}", body);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesProject_WhenExists()
    {
        var project = await _factory.WithDbContextAsync(async db =>
            await TestDataSeeder.CreateProjectAsync(db, await TestDataSeeder.CreateWorkspaceAsync(db)));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await _factory.WithDbContextAsync(db => DbAssert.ProjectExistsAsync(db, project.Id)));
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/projects/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Returns401_WhenAnonymous()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
