using System.ComponentModel.DataAnnotations;

namespace FlowCore.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }

    public virtual ICollection<Workspace> OwnedWorkspaces { get; set; } = new List<Workspace>();
    public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}
