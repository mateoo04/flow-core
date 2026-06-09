using Microsoft.AspNetCore.Http;

namespace FlowCore.Services.Attachments;

public interface IAttachmentStorage
{
    // Persists the file under a server-generated key and returns that relative key.
    Task<string> SaveAsync(Guid taskId, IFormFile file, CancellationToken ct = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default);

    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
