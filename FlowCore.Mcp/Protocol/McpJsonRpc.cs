using System.Text.Json;

namespace FlowCore.Mcp.Protocol;

internal static class McpJsonRpc
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static Task WriteResultAsync(JsonElement id, object? result) =>
        Console.Out.WriteLineAsync(JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result }, JsonOptions));

    public static Task WriteErrorAsync(JsonElement? id, int code, string message) =>
        Console.Out.WriteLineAsync(JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code, message } }, JsonOptions));

    public static JsonElement? TryGetId(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.TryGetProperty("id", out var id) ? id.Clone() : null;
        }
        catch (JsonException) { return null; }
    }
}
