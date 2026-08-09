using FlowCore.Services.Attachments;
using FlowCore.Validation;
using Microsoft.Extensions.Options;
using Xunit;

namespace FlowCore.Tests.Attachments;

public class ImageUploadValidatorTests
{
    private static ImageUploadValidator NewValidator(long maxBytes = 5 * 1024 * 1024) =>
        new(Options.Create(new AttachmentOptions { MaxBytes = maxBytes }));

    [Fact]
    public void Validate_AllowsPng()
    {
        var result = NewValidator().Validate(new ImageUpload("photo.png", "image/png", 1000));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsNonImageExtension()
    {
        var result = NewValidator().Validate(new ImageUpload("notes.txt", "text/plain", 1000));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsExtensionContentTypeMismatch()
    {
        var result = NewValidator().Validate(new ImageUpload("photo.png", "application/pdf", 1000));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsOversize()
    {
        var result = NewValidator(maxBytes: 500).Validate(new ImageUpload("photo.png", "image/png", 1000));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmpty()
    {
        var result = NewValidator().Validate(new ImageUpload("photo.png", "image/png", 0));
        Assert.False(result.IsValid);
    }
}
