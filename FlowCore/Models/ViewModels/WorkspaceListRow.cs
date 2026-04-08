using FlowCore.Models;

namespace FlowCore.Models.ViewModels;

public sealed record WorkspaceListRow(
    Guid Id,
    string Name,
    WorkspaceVisibility Visibility,
    int ProjectCount);
