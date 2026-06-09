using Microsoft.Extensions.Options;

namespace FlowCore.Services.Attachments;

public readonly record struct UploadValidationResult(bool IsValid, string? Error)
{
    public static UploadValidationResult Ok() => new(true, null);
    public static UploadValidationResult Fail(string error) => new(false, error);
}

public sealed class ImageUploadValidator
{
    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
    };

    private static readonly Dictionary<string, string> ContentTypeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    private readonly long _maxBytes;

    public ImageUploadValidator(IOptions<AttachmentOptions> options) => _maxBytes = options.Value.MaxBytes;

    public UploadValidationResult Validate(string fileName, string contentType, long length)
    {
        if (length <= 0)
            return UploadValidationResult.Fail("File is empty.");

        if (length > _maxBytes)
            return UploadValidationResult.Fail($"File exceeds the {_maxBytes / (1024 * 1024)} MB limit.");

        if (!ExtensionByContentType.TryGetValue(contentType, out _))
            return UploadValidationResult.Fail("Only image files are allowed.");

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !ContentTypeByExtension.TryGetValue(ext, out var expected))
            return UploadValidationResult.Fail("Only image files are allowed.");

        if (!string.Equals(expected, contentType, StringComparison.OrdinalIgnoreCase))
            return UploadValidationResult.Fail("File extension does not match its content type.");

        return UploadValidationResult.Ok();
    }
}
