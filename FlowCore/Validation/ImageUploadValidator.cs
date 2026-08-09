using FlowCore.Services.Attachments;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace FlowCore.Validation;

public sealed record ImageUpload(string FileName, string ContentType, long Length);

public sealed class ImageUploadValidator : AbstractValidator<ImageUpload>
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

    public ImageUploadValidator(IOptions<AttachmentOptions> options)
    {
        var maxBytes = options.Value.MaxBytes;

        RuleFor(upload => upload.Length)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .WithMessage("File is empty.")
            .LessThanOrEqualTo(maxBytes)
            .WithMessage($"File exceeds the {maxBytes / (1024 * 1024)} MB limit.");

        RuleFor(upload => upload.ContentType)
            .Must(HasAllowedContentType)
            .WithMessage("Only image files are allowed.");

        RuleFor(upload => upload.FileName)
            .Must(HasAllowedExtension)
            .WithMessage("Only image files are allowed.");

        RuleFor(upload => upload)
            .Must(ExtensionMatchesContentType)
            .When(upload => HasAllowedContentType(upload.ContentType) && HasAllowedExtension(upload.FileName))
            .WithMessage("File extension does not match its content type.");
    }

    private static bool HasAllowedContentType(string contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && ExtensionByContentType.ContainsKey(contentType);

    private static bool HasAllowedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && ContentTypeByExtension.ContainsKey(ext);
    }

    private static bool ExtensionMatchesContentType(ImageUpload upload)
    {
        var ext = Path.GetExtension(upload.FileName);
        return ContentTypeByExtension.TryGetValue(ext, out var expected)
            && string.Equals(expected, upload.ContentType, StringComparison.OrdinalIgnoreCase);
    }
}
