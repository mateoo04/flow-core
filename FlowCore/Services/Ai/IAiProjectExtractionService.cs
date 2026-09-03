using FlowCore.Models.Ai;

namespace FlowCore.Services.Ai;

public interface IAiProjectExtractionService
{
    Task<AiProjectDraft> ExtractAsync(string prompt, CancellationToken ct = default);
}
