using System.Net;
using System.Net.Http.Json;
using FlowCore.Models.Dtos;
using FlowCore.Tests.Infrastructure;
using Xunit;

namespace FlowCore.Tests.Api;

public class BoardsApiTests : IClassFixture<FlowCoreApiFactory>
{
    private readonly FlowCoreApiFactory _factory;

    public BoardsApiTests(FlowCoreApiFactory factory) => _factory = factory;

    private Task<FlowCore.Models.Board> SeedBoardAsync() =>
        _factory.WithDbContextAsync(async db =>
        {
            var user = await TestDataSeeder.EnsureTestUserAsync(db);
            var ws = await TestDataSeeder.CreateWorkspaceAsync(db);
            await TestDataSeeder.AddMemberAsync(db, ws, user.Id);
            var project = await TestDataSeeder.CreateProjectAsync(db, ws);
            return await TestDataSeeder.CreateBoardAsync(db, project);
        });

    [Fact]
    public async Task GetAll_ReturnsOkAndSeededBoard()
    {
        var board = await SeedBoardAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/boards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<BoardDto>>();
        Assert.NotNull(items);
        Assert.Contains(items!, b => b.Id == board.Id);
    }

    [Fact]
    public async Task GetById_ReturnsBoard_WhenExists()
    {
        var board = await SeedBoardAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/boards/{board.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<BoardDto>();
        Assert.Equal(board.Id, dto!.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/boards/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesBoard_Returns201()
    {
        var board = await SeedBoardAsync();
        var client = _factory.CreateAuthenticatedClient();
        var body = new BoardCreateDto { ProjectId = board.ProjectId, Name = "New Board", Position = 1 };

        var response = await client.PostAsJsonAsync("/api/boards", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<BoardDto>();
        Assert.Equal("New Board", dto!.Name);
        Assert.True(await _factory.WithDbContextAsync(db => DbAssert.BoardExistsAsync(db, dto.Id)));
    }

    [Fact]
    public async Task Post_Returns400_WhenInvalid()
    {
        var board = await SeedBoardAsync();
        var client = _factory.CreateAuthenticatedClient();
        var body = new BoardCreateDto { ProjectId = board.ProjectId, Name = "" };

        var response = await client.PostAsJsonAsync("/api/boards", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenProjectMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new BoardCreateDto { ProjectId = Guid.NewGuid(), Name = "Orphan" };

        var response = await client.PostAsJsonAsync("/api/boards", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesBoard_WhenExists()
    {
        var board = await SeedBoardAsync();
        var client = _factory.CreateAuthenticatedClient();
        var body = new BoardUpdateDto { Name = "Renamed", Position = 5, IsDefault = false };

        var response = await client.PutAsJsonAsync($"/api/boards/{board.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<BoardDto>();
        Assert.Equal("Renamed", dto!.Name);
        Assert.Equal(5, dto.Position);
    }

    [Fact]
    public async Task Put_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new BoardUpdateDto { Name = "Renamed" };
        var response = await client.PutAsJsonAsync($"/api/boards/{Guid.NewGuid()}", body);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesBoard_WhenExists()
    {
        var board = await SeedBoardAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/boards/{board.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await _factory.WithDbContextAsync(db => DbAssert.BoardExistsAsync(db, board.Id)));
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/boards/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Returns401_WhenAnonymous()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/boards");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
