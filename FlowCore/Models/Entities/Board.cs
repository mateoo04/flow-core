using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowCore.Models;

public class Board
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(Project))]
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Project? Project { get; set; }
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
