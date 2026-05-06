using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowCore.Models;

public class Project
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(Workspace))]
    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public ProjectStatus Status { get; set; }
    public ProjectPriority Priority { get; set; }

    public virtual Workspace? Workspace { get; set; }
    public virtual ICollection<Board> Boards { get; set; } = new List<Board>();
}
