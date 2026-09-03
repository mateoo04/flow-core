namespace FlowCore.Models.Ai;

public sealed class AiCreateTaskRequest
{
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string? Priority { get; init; }
    public string? DueDate { get; init; }
}
