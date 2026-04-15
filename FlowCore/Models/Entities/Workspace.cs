namespace FlowCore.Models;

public class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public WorkspaceVisibility Visibility { get; set; }
    public Guid OwnerUserId { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<TaskStatusDefinition> TaskStatusDefinitions { get; set; } = new List<TaskStatusDefinition>();
}
