namespace FlowCore.Services.Attachments;

public sealed class AttachmentOptions
{
    public const string SectionName = "Attachments";

    public string StoragePath { get; set; } = Path.Combine("wwwroot", "uploads");

    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
}
