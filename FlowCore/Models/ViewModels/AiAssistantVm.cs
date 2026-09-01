namespace FlowCore.Models.ViewModels;

public sealed record AiAssistantVm(IReadOnlyList<AiProjectOptionVm> Projects, Guid? ActiveProjectId);

public sealed record AiProjectOptionVm(Guid Id, string Name);
