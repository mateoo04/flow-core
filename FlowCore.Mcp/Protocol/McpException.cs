namespace FlowCore.Mcp.Protocol;

internal sealed class McpException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}
