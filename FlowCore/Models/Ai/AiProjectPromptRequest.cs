namespace FlowCore.Models.Ai;

public sealed class AiProjectPromptRequest
{
    public Guid WorkspaceId { get; init; }
    public string Prompt { get; init; } = "";
}
