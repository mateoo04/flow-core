namespace FlowCore.Models;

public class TaskAssignment
{
    public Guid TaskItemId { get; set; }
    public Guid UserId { get; set; }
    public TaskRole Role { get; set; }
    public DateTime AssignedAt { get; set; }

    public TaskItem? TaskItem { get; set; }
    public User? User { get; set; }
}
