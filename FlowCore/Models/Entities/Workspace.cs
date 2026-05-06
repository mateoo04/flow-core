using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowCore.Models;

public class Workspace
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public WorkspaceVisibility Visibility { get; set; }

    [ForeignKey(nameof(Owner))]
    public Guid OwnerUserId { get; set; }

    public virtual User? Owner { get; set; }
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<TaskStatusDefinition> TaskStatusDefinitions { get; set; } = new List<TaskStatusDefinition>();
}
