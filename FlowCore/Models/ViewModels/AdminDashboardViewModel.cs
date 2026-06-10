namespace FlowCore.Models.ViewModels;

public sealed record AdminUserRow(Guid Id, string Email, string FullName, bool IsActive, IReadOnlyList<string> Roles);

public sealed record AdminWorkspaceRow(Guid Id, string Name, WorkspaceVisibility Visibility, int MemberCount, int ProjectCount);

public sealed record AdminDashboardViewModel(
    IReadOnlyList<AdminUserRow> Users,
    IReadOnlyList<AdminWorkspaceRow> Workspaces);
