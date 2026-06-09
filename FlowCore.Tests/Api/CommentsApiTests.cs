using System.Net;
using System.Net.Http.Json;
using FlowCore.Models.Dtos;
using FlowCore.Tests.Infrastructure;
using Xunit;

namespace FlowCore.Tests.Api;

public class CommentsApiTests : IClassFixture<FlowCoreApiFactory>
{
    private readonly FlowCoreApiFactory _factory;

    public CommentsApiTests(FlowCoreApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAll_ReturnsOkAndSeededComment()
    {
        var comment = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateCommentAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<CommentDto>>();
        Assert.NotNull(items);
        Assert.Contains(items!, c => c.Id == comment.Id);
    }

    [Fact]
    public async Task GetById_ReturnsCommentWithAuthor_WhenExists()
    {
        var comment = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateCommentAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.Equal(comment.Id, dto!.Id);
        Assert.NotNull(dto.Author);
        Assert.Equal(comment.AuthorUserId, dto.Author!.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/comments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesComment_Returns201()
    {
        var (taskId, userId) = await _factory.WithDbContextAsync(async db =>
        {
            var user = await TestDataSeeder.EnsureTestUserAsync(db);
            var task = await TestDataSeeder.CreateTaskAsync(db);
            return (task.Id, user.Id);
        });
        var client = _factory.CreateAuthenticatedClient();
        var body = new CommentCreateDto { TaskItemId = taskId, AuthorUserId = userId, Body = "Hello" };

        var response = await client.PostAsJsonAsync("/api/comments", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.Equal("Hello", dto!.Body);
        Assert.True(await _factory.WithDbContextAsync(db => DbAssert.CommentExistsAsync(db, dto.Id)));
    }

    [Fact]
    public async Task Post_Returns400_WhenInvalid()
    {
        var (taskId, userId) = await _factory.WithDbContextAsync(async db =>
        {
            var user = await TestDataSeeder.EnsureTestUserAsync(db);
            var task = await TestDataSeeder.CreateTaskAsync(db);
            return (task.Id, user.Id);
        });
        var client = _factory.CreateAuthenticatedClient();
        var body = new CommentCreateDto { TaskItemId = taskId, AuthorUserId = userId, Body = "" };

        var response = await client.PostAsJsonAsync("/api/comments", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenTaskMissing()
    {
        var userId = await _factory.WithDbContextAsync(async db => (await TestDataSeeder.EnsureTestUserAsync(db)).Id);
        var client = _factory.CreateAuthenticatedClient();
        var body = new CommentCreateDto { TaskItemId = Guid.NewGuid(), AuthorUserId = userId, Body = "Hi" };

        var response = await client.PostAsJsonAsync("/api/comments", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesComment_WhenExists()
    {
        var comment = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateCommentAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new CommentUpdateDto { Body = "Edited" };

        var response = await client.PutAsJsonAsync($"/api/comments/{comment.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<CommentDto>();
        Assert.Equal("Edited", dto!.Body);
        Assert.NotNull(dto.EditedAt);
    }

    [Fact]
    public async Task Put_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new CommentUpdateDto { Body = "Edited" };
        var response = await client.PutAsJsonAsync($"/api/comments/{Guid.NewGuid()}", body);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesComment_WhenExists()
    {
        var comment = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateCommentAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await _factory.WithDbContextAsync(db => DbAssert.CommentExistsAsync(db, comment.Id)));
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/comments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Returns401_WhenAnonymous()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/comments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
