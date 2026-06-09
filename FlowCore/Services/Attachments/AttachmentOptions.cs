namespace FlowCore.Services.Attachments;

public sealed class AttachmentOptions
{
    public const string SectionName = "Attachments";

    // Root directory uploads are written under. Local default; Railway sets /data/uploads via env.
    public string StoragePath { get; set; } = Path.Combine("wwwroot", "uploads");

    public long MaxBytes { get; set; } = 5 * 1024 * 1024; // 5 MB
}
