namespace FlowCore.Models.ViewModels;

public sealed record WorkspaceListRow(
    Guid Id,
    string Name,
    int ProjectCount);
