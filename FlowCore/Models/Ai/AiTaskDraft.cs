namespace FlowCore.Models.Ai;

public sealed record AiTaskDraft(string Title, string? Description, TaskPriority Priority, DateTime? DueDate);
