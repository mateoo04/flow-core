namespace FlowCore.Models.Ai;

public sealed class AiCreateProjectRequest
{
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? StartDate { get; init; }
    public string? DueDate { get; init; }
}
