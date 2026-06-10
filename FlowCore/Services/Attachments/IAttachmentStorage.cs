using Microsoft.AspNetCore.Http;

namespace FlowCore.Services.Attachments;

public interface IAttachmentStorage
{
    Task<string> SaveAsync(Guid taskId, IFormFile file, CancellationToken ct = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default);

    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
