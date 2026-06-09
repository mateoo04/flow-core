using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowCore.Models;

public class Attachment
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(TaskItem))]
    public Guid TaskItemId { get; set; }

    public string FileName { get; set; } = string.Empty;      // original client name
    public string StoragePath { get; set; } = string.Empty;   // relative key within the storage root
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    [ForeignKey(nameof(UploadedBy))]
    public Guid UploadedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual TaskItem? TaskItem { get; set; }
    public virtual User? UploadedBy { get; set; }
}
