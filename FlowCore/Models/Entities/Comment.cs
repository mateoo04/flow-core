using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowCore.Models;

public class Comment
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(TaskItem))]
    public Guid TaskItemId { get; set; }

    [ForeignKey(nameof(Author))]
    public Guid AuthorUserId { get; set; }

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }

    public virtual TaskItem? TaskItem { get; set; }
    public virtual User? Author { get; set; }
}
