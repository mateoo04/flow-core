namespace FlowCore.Models.ViewModels;

public sealed record AiAssistantVm(
    IReadOnlyList<AiProjectOptionVm> Projects,
    IReadOnlyList<AiWorkspaceOptionVm> Workspaces,
    Guid? ActiveProjectId,
    Guid? ActiveWorkspaceId);

public sealed record AiProjectOptionVm(Guid Id, string Name);

public sealed record AiWorkspaceOptionVm(Guid Id, string Name);
