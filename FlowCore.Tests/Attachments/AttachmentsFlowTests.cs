using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FlowCore.Models;
using FlowCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlowCore.Tests.Attachments;

public class AttachmentsFlowTests : IClassFixture<FlowCoreApiFactory>
{
    private readonly FlowCoreApiFactory _factory;

    public AttachmentsFlowTests(FlowCoreApiFactory factory) => _factory = factory;

    private async Task<Guid> SeedTaskWithMembershipAsync()
    {
        return await _factory.WithDbContextAsync(async db =>
        {
            var user = await TestDataSeeder.EnsureTestUserAsync(db);
            var ctx = await TestDataSeeder.CreateTaskContextAsync(db);
            await TestDataSeeder.AddMemberAsync(db, ctx.Workspace, user.Id);
            var task = await TestDataSeeder.CreateTaskAsync(db, ctx);
            return task.Id;
        });
    }

    private static MultipartFormDataContent ImageContent(string name = "pic.png")
    {
        var bytes = Encoding.UTF8.GetBytes("fake-image-bytes");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent { { fileContent, "file", name } };
        return form;
    }

    [Fact]
    public async Task Upload_Then_List_Then_Delete()
    {
        var taskId = await SeedTaskWithMembershipAsync();
        var client = _factory.CreateAuthenticatedClient();

        var upload = await client.PostAsync($"/tasks/{taskId}/attachments", ImageContent());
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var attachmentId = await _factory.WithDbContextAsync(db =>
            db.Attachments.Where(a => a.TaskItemId == taskId).Select(a => a.Id).FirstAsync());

        var list = await client.GetAsync($"/tasks/{taskId}/attachments");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var html = await list.Content.ReadAsStringAsync();
        Assert.Contains(attachmentId.ToString(), html);

        var content = await client.GetAsync($"/attachments/{attachmentId}/content");
        Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        Assert.Equal("fake-image-bytes", await content.Content.ReadAsStringAsync());

        var delete = await client.PostAsync($"/attachments/{attachmentId}/delete", null);
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.False(await _factory.WithDbContextAsync(db => db.Attachments.AnyAsync(a => a.Id == attachmentId)));
    }

    [Fact]
    public async Task Upload_Returns400_ForNonImage()
    {
        var taskId = await SeedTaskWithMembershipAsync();
        var client = _factory.CreateAuthenticatedClient();

        var bytes = Encoding.UTF8.GetBytes("not an image");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var form = new MultipartFormDataContent { { fileContent, "file", "notes.txt" } };

        var response = await client.PostAsync($"/tasks/{taskId}/attachments", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_Returns401_WhenAnonymous()
    {
        var taskId = await SeedTaskWithMembershipAsync();
        var client = _factory.CreateAnonymousClient();

        var response = await client.PostAsync($"/tasks/{taskId}/attachments", ImageContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Content_Returns403_ForNonMember()
    {
        var attachmentId = await _factory.WithDbContextAsync(async db =>
        {
            var user = await TestDataSeeder.EnsureTestUserAsync(db);
            var ctx = await TestDataSeeder.CreateTaskContextAsync(db);
            var task = await TestDataSeeder.CreateTaskAsync(db, ctx);
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                TaskItemId = task.Id,
                FileName = "secret.png",
                StoragePath = $"tasks/{task.Id}/secret.png",
                ContentType = "image/png",
                FileSize = 10,
                UploadedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.Attachments.Add(attachment);
            await db.SaveChangesAsync();
            return attachment.Id;
        });
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/attachments/{attachmentId}/content");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
