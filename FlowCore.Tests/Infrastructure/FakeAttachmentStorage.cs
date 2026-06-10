using System.Collections.Concurrent;
using FlowCore.Services.Attachments;
using Microsoft.AspNetCore.Http;

namespace FlowCore.Tests.Infrastructure;

public sealed class FakeAttachmentStorage : IAttachmentStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public async Task<string> SaveAsync(Guid taskId, IFormFile file, CancellationToken ct = default)
    {
        var key = $"tasks/{taskId}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        _files[key] = ms.ToArray();
        return key;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        if (!_files.TryGetValue(storagePath, out var bytes))
            throw new FileNotFoundException(storagePath);
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        _files.TryRemove(storagePath, out _);
        return Task.CompletedTask;
    }

    public bool Contains(string storagePath) => _files.ContainsKey(storagePath);
}
