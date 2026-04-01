namespace FlowCore.Models;

public class Comment
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }

    public TaskItem? TaskItem { get; set; }
}
