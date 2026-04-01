namespace FlowCore.Models;

public class Project
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public ProjectStatus Status { get; set; }
    public ProjectPriority Priority { get; set; }

    public Workspace? Workspace { get; set; }
    public ICollection<Board> Boards { get; set; } = new List<Board>();
    public ICollection<TaskStatusDefinition> TaskStatusDefinitions { get; set; } = new List<TaskStatusDefinition>();
}
