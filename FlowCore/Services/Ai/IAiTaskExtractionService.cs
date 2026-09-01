using FlowCore.Models;

namespace FlowCore.Services.Ai;

public interface IAiTaskExtractionService
{
    Task<AiTaskDraft> ExtractAsync(string prompt, CancellationToken ct = default);
}

public sealed record AiTaskDraft(string Title, string? Description, TaskPriority Priority, DateTime? DueDate);
