namespace FlowCore.Models;

public class BoardColumn
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public int WipLimit { get; set; }
    public bool IsDoneColumn { get; set; }
    public DateTime CreatedAt { get; set; }

    public Board? Board { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
