using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowCore.Models;

public class TaskStatusDefinition
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(Workspace))]
    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsDoneState { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Workspace? Workspace { get; set; }
    public virtual ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
}
