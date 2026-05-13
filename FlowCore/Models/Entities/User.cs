using Microsoft.AspNetCore.Identity;

namespace FlowCore.Models;

public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }

    public virtual ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = new List<WorkspaceMember>();
    public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}
