using System.Net;
using System.Net.Http.Json;
using FlowCore.Models.Dtos;
using FlowCore.Tests.Infrastructure;
using Xunit;

namespace FlowCore.Tests.Api;

public class TasksApiTests : IClassFixture<FlowCoreApiFactory>
{
    private readonly FlowCoreApiFactory _factory;

    public TasksApiTests(FlowCoreApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAll_ReturnsOkAndSeededTask()
    {
        var task = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTaskAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<TaskItemDto>>();
        Assert.NotNull(items);
        Assert.Contains(items!, t => t.Id == task.Id);
    }

    [Fact]
    public async Task GetById_ReturnsTaskWithNestedStatus_WhenExists()
    {
        var task = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTaskAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.Equal(task.Id, dto!.Id);
        Assert.NotNull(dto.Status);
        Assert.Equal(task.TaskStatusDefinitionId, dto.Status!.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesTaskWithAssignee_Returns201()
    {
        var (ctx, userId) = await _factory.WithDbContextAsync(async db =>
        {
            var context = await TestDataSeeder.CreateTaskContextAsync(db);
            var user = await TestDataSeeder.EnsureTestUserAsync(db);
            return (context, user.Id);
        });
        var client = _factory.CreateAuthenticatedClient();
        var body = new TaskCreateDto
        {
            BoardId = ctx.Board.Id,
            TaskStatusDefinitionId = ctx.Status.Id,
            Title = "New Task",
            Description = "d",
            AssigneeIds = new List<Guid> { userId }
        };

        var response = await client.PostAsJsonAsync("/api/tasks", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.Equal("New Task", dto!.Title);
        Assert.Contains(dto.Assignees, a => a.Id == userId);
        Assert.True(await _factory.WithDbContextAsync(db => DbAssert.TaskExistsAsync(db, dto.Id)));
    }

    [Fact]
    public async Task Post_Returns400_WhenInvalid()
    {
        var ctx = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTaskContextAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new TaskCreateDto { BoardId = ctx.Board.Id, TaskStatusDefinitionId = ctx.Status.Id, Title = "" };

        var response = await client.PostAsJsonAsync("/api/tasks", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenBoardMissing()
    {
        var ctx = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTaskContextAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new TaskCreateDto { BoardId = Guid.NewGuid(), TaskStatusDefinitionId = ctx.Status.Id, Title = "X" };

        var response = await client.PostAsJsonAsync("/api/tasks", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesTask_WhenExists()
    {
        var task = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTaskAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new TaskUpdateDto
        {
            TaskStatusDefinitionId = task.TaskStatusDefinitionId,
            Title = "Renamed",
            Description = "x",
            StoryPoints = 8
        };

        var response = await client.PutAsJsonAsync($"/api/tasks/{task.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.Equal("Renamed", dto!.Title);
        Assert.Equal(8, dto.StoryPoints);
    }

    [Fact]
    public async Task Put_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new TaskUpdateDto { TaskStatusDefinitionId = Guid.NewGuid(), Title = "Renamed" };
        var response = await client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", body);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTask_WhenExists()
    {
        var task = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTaskAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await _factory.WithDbContextAsync(db => DbAssert.TaskExistsAsync(db, task.Id)));
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Returns401_WhenAnonymous()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
