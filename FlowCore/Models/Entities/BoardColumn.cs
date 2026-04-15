namespace FlowCore.Models;

public class BoardColumn
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public int WipLimit { get; set; }
    public bool IsDoneColumn { get; set; }
    /// <summary>Swimlane accent for Kanban column header (e.g. #3b82f6). Empty uses a neutral default in the UI.</summary>
    public string ColorHex { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Board? Board { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
