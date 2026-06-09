using System.Text;
using FlowCore.Services.Attachments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace FlowCore.Tests.Attachments;

public class LocalDiskAttachmentStorageTests
{
    private static LocalDiskAttachmentStorage NewStorage(out string root)
    {
        root = Path.Combine(Path.GetTempPath(), $"flowcore-att-{Guid.NewGuid()}");
        var options = Options.Create(new AttachmentOptions { StoragePath = root });
        return new LocalDiskAttachmentStorage(options);
    }

    private static IFormFile FakeImage(string name = "pic.png")
    {
        var bytes = Encoding.UTF8.GetBytes("fake-image-bytes");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = "image/png" };
    }

    [Fact]
    public async Task Save_Then_OpenRead_RoundTrips()
    {
        var storage = NewStorage(out _);
        var taskId = Guid.NewGuid();

        var key = await storage.SaveAsync(taskId, FakeImage());

        Assert.Contains(taskId.ToString(), key);
        await using var read = await storage.OpenReadAsync(key);
        using var reader = new StreamReader(read);
        Assert.Equal("fake-image-bytes", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Delete_RemovesFile()
    {
        var storage = NewStorage(out _);
        var key = await storage.SaveAsync(Guid.NewGuid(), FakeImage());

        await storage.DeleteAsync(key);

        await Assert.ThrowsAsync<FileNotFoundException>(() => storage.OpenReadAsync(key));
    }
}
