using System.ComponentModel.DataAnnotations;

namespace FlowCore.Models;

public class Workspace
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<TaskStatusDefinition> TaskStatusDefinitions { get; set; } = new List<TaskStatusDefinition>();
}
