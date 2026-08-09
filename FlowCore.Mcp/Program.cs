using System.Text.Json;
using FlowCore.Data;
using FlowCore.Mcp.Configuration;
using FlowCore.Mcp.Protocol;
using FlowCore.Mcp.Services;
using Microsoft.EntityFrameworkCore;

// This is a local STDIO MCP server. stdout is reserved for JSON-RPC messages.
DotEnvLoader.Load();

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FlowCoreDbContext")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("Set ConnectionStrings__FlowCoreDbContext or DATABASE_URL before starting FlowCore MCP.");
var userEmail = Environment.GetEnvironmentVariable("FLOWCORE_MCP_USER_EMAIL")
    ?? throw new InvalidOperationException("Set FLOWCORE_MCP_USER_EMAIL to the FlowCore account that this local MCP server represents.");

var dbOptions = new DbContextOptionsBuilder<FlowCoreDbContext>()
    .UseNpgsql(PostgresConnectionStringResolver.Resolve(connectionString))
    .Options;
var flowCore = new FlowCoreMcpService(dbOptions, userEmail);

while (await Console.In.ReadLineAsync() is { } line)
{
    if (string.IsNullOrWhiteSpace(line)) continue;

    try
    {
        using var document = JsonDocument.Parse(line);
        var request = document.RootElement;
        if (!request.TryGetProperty("method", out var methodElement)) continue;

        var parameters = request.TryGetProperty("params", out var value) ? value : default;
        var result = methodElement.GetString() switch
        {
            "initialize" => FlowCoreToolCatalog.Initialize(parameters),
            "notifications/initialized" => null,
            "tools/list" => FlowCoreToolCatalog.List(),
            "tools/call" => await flowCore.CallToolAsync(parameters),
            var method => throw new McpException(-32601, $"Method '{method}' is not supported.")
        };

        if (request.TryGetProperty("id", out var id))
            await McpJsonRpc.WriteResultAsync(id, result);
    }
    catch (McpException ex)
    {
        await McpJsonRpc.WriteErrorAsync(McpJsonRpc.TryGetId(line), ex.Code, ex.Message);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FlowCore MCP error: {ex.GetType().Name}: {ex.Message}");
        await McpJsonRpc.WriteErrorAsync(McpJsonRpc.TryGetId(line), -32603, "The FlowCore MCP server could not complete the request.");
    }
}
