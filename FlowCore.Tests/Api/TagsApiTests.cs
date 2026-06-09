using System.Net;
using System.Net.Http.Json;
using FlowCore.Models.Dtos;
using FlowCore.Tests.Infrastructure;
using Xunit;

namespace FlowCore.Tests.Api;

public class TagsApiTests : IClassFixture<FlowCoreApiFactory>
{
    private readonly FlowCoreApiFactory _factory;

    public TagsApiTests(FlowCoreApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAll_ReturnsOkAndSeededTag()
    {
        var tag = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTagAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>();
        Assert.NotNull(tags);
        Assert.Contains(tags!, t => t.Id == tag.Id);
    }

    [Fact]
    public async Task GetById_ReturnsTag_WhenExists()
    {
        var tag = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTagAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/tags/{tag.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(dto);
        Assert.Equal(tag.Id, dto!.Id);
        Assert.Equal(tag.Name, dto.Name);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/tags/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesTag_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new TagCreateDto { Name = "API Tag", ColorHex = "#abcdef" };

        var response = await client.PostAsJsonAsync("/api/tags", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(dto);
        Assert.NotEqual(Guid.Empty, dto!.Id);
        Assert.Equal("API Tag", dto.Name);

        var persisted = await _factory.WithDbContextAsync(db =>
            FlowCore.Tests.Infrastructure.DbAssert.TagExistsAsync(db, dto.Id));
        Assert.True(persisted);
    }

    [Fact]
    public async Task Post_Returns400_WhenInvalid()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new TagCreateDto { Name = "", ColorHex = "not-a-color" };

        var response = await client.PostAsJsonAsync("/api/tags", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesTag_WhenExists()
    {
        var tag = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTagAsync(db));
        var client = _factory.CreateAuthenticatedClient();
        var body = new TagUpdateDto { Name = "Renamed", ColorHex = "#123456" };

        var response = await client.PutAsJsonAsync($"/api/tags/{tag.Id}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(dto);
        Assert.Equal("Renamed", dto!.Name);
        Assert.Equal("#123456", dto.ColorHex);
    }

    [Fact]
    public async Task Put_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();
        var body = new TagUpdateDto { Name = "Renamed", ColorHex = "#123456" };

        var response = await client.PutAsJsonAsync($"/api/tags/{Guid.NewGuid()}", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTag_WhenExists()
    {
        var tag = await _factory.WithDbContextAsync(db => TestDataSeeder.CreateTagAsync(db));
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/tags/{tag.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stillExists = await _factory.WithDbContextAsync(db =>
            FlowCore.Tests.Infrastructure.DbAssert.TagExistsAsync(db, tag.Id));
        Assert.False(stillExists);
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/tags/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Returns401_WhenAnonymous()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
