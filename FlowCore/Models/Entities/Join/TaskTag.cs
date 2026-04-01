namespace FlowCore.Models;

public class TaskTag
{
    public Guid TaskItemId { get; set; }
    public Guid TagId { get; set; }
    public DateTime LinkedAt { get; set; }

    public TaskItem? TaskItem { get; set; }
    public Tag? Tag { get; set; }
}
