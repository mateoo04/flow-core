using FlowCore.Models.Ai;

namespace FlowCore.Services.Ai;

public interface IAiTaskExtractionService
{
    Task<AiTaskDraft> ExtractAsync(string prompt, CancellationToken ct = default);
}
