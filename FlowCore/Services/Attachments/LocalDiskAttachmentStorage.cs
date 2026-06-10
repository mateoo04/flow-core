using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FlowCore.Services.Attachments;

public sealed class LocalDiskAttachmentStorage : IAttachmentStorage
{
    private readonly string _root;

    public LocalDiskAttachmentStorage(IOptions<AttachmentOptions> options)
    {
        _root = Path.GetFullPath(options.Value.StoragePath);
    }

    public async Task<string> SaveAsync(Guid taskId, IFormFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.FileName);
        var key = Path.Combine("tasks", taskId.ToString(), $"{Guid.NewGuid()}{ext}");
        var absolute = Path.Combine(_root, key);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);

        await using var stream = new FileStream(absolute, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        return key;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        var absolute = ResolveInsideRoot(storagePath);
        Stream stream = new FileStream(absolute, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var absolute = ResolveInsideRoot(storagePath);
        if (File.Exists(absolute))
            File.Delete(absolute);
        return Task.CompletedTask;
    }

    private string ResolveInsideRoot(string storagePath)
    {
        var absolute = Path.GetFullPath(Path.Combine(_root, storagePath));
        if (!absolute.StartsWith(_root, StringComparison.Ordinal))
            throw new InvalidOperationException("Attachment path escapes the storage root.");
        return absolute;
    }
}
