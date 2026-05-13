namespace FlowCore.Models;

public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public WorkspaceRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
