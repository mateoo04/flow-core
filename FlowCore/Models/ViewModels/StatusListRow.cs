namespace FlowCore.Models.ViewModels;

public sealed record StatusListRow(
    Guid Id,
    string Name,
    string ColorHex,
    int Position,
    bool IsDoneState,
    Guid WorkspaceId,
    string WorkspaceName);
