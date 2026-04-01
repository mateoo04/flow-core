namespace FlowCore.Models;

public class TaskStatusDefinition
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDoneState { get; set; }
    public DateTime CreatedAt { get; set; }

    public Project? Project { get; set; }
    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
}
