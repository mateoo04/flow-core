namespace FlowCore.Models.Ai;

public sealed class AiTaskPromptRequest
{
    public Guid ProjectId { get; init; }
    public string Prompt { get; init; } = "";
}
