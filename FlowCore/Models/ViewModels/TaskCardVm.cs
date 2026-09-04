namespace FlowCore.Models.ViewModels;

public enum TaskCardLayout
{
    MyTasksActive,
    Board
}

public sealed class TaskCardVm
{
    public required Guid TaskId { get; init; }
    public required string Title { get; init; }
    public required string AccentHex { get; init; }
    public required TaskCardLayout Layout { get; init; }
    public TaskAssigneeStackVm Assignees { get; init; } = new();

    public string? ProjectName { get; init; }

    public string? StatusNameForScreenReader { get; init; }
    public int SubtaskTotal { get; init; }
    public int SubtaskDone { get; init; }
    public string? DueDateLabel { get; init; }
    public bool IsOverdue { get; init; }
    public string? PriorityLabel { get; init; }
    public string PriorityPillClasses { get; init; } = "";
}
