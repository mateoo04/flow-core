namespace FlowCore.Models.ViewModels;

public sealed record UserListRow(Guid Id, string FullName, string Email, bool IsActive);
