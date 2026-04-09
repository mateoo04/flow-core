namespace FlowCore.Models.ViewModels;

public sealed class MyWorkViewModel
{
    public string CurrentUserDisplayName { get; init; } = "";
    public IReadOnlyList<StatusTaskGroupVm> StatusGroups { get; init; } = Array.Empty<StatusTaskGroupVm>();
}

public sealed class StatusTaskGroupVm
{
    public string StatusName { get; init; } = "";
    public string? StatusColorHex { get; init; }
    public int SortKey { get; init; }
    public IReadOnlyList<MyTaskCardVm> Tasks { get; init; } = Array.Empty<MyTaskCardVm>();
}

public sealed class MyTaskCardVm
{
    public Guid TaskId { get; init; }
    public string Title { get; init; } = "";
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = "";
    public TaskAssigneeStackVm Assignees { get; init; } = new();
}

public sealed class TaskAssigneeStackVm
{
    public IReadOnlyList<AssigneeAvatarVm> Shown { get; init; } = Array.Empty<AssigneeAvatarVm>();
    public int ExtraCount { get; init; }
}

public sealed class AssigneeAvatarVm
{
    public string Initials { get; init; } = "";
    public string BackgroundHex { get; init; } = "#6366f1";
}
