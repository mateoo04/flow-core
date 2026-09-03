namespace FlowCore.Models.Ai;

public sealed record AiProjectDraft(
    string Name,
    string? Description,
    ProjectStatus Status,
    ProjectPriority Priority,
    DateTime? StartDate,
    DateTime? DueDate);
