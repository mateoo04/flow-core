using System.Text.Json;

namespace FlowCore.Mcp.Protocol;

internal static class FlowCoreToolCatalog
{
    public static object Initialize(JsonElement parameters)
    {
        var protocolVersion = parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("protocolVersion", out var version)
            ? version.GetString()
            : null;

        return new
        {
            protocolVersion = protocolVersion ?? "2025-03-26",
            capabilities = new { tools = new { } },
            serverInfo = new { name = "flowcore-mcp", version = "1.0.0" },
            instructions = "Use FlowCore tools only for the configured user's workspaces. Confirm intent before calling a tool that creates or changes a task."
        };
    }

    public static object List() => new
    {
        tools = new object[]
        {
            Tool("list_projects", "List projects visible to the connected FlowCore user.", new { type = "object", properties = new { }, additionalProperties = false }, true),
            Tool("search_tasks", "Search tasks visible to the connected user by title or description.", new { type = "object", properties = new { query = new { type = "string", description = "Optional text to search for." } }, additionalProperties = false }, true),
            Tool("get_project_board", "Show boards, statuses, and tasks for one visible project.", IdSchema("project_id", "The FlowCore project UUID."), true),
            Tool("create_task", "Create a task on a board that belongs to a visible project.", new
            {
                type = "object",
                properties = new { board_id = new { type = "string", description = "The FlowCore board UUID." }, status_id = new { type = "string", description = "The status UUID from the same workspace." }, title = new { type = "string", description = "Short task title." }, description = new { type = "string", description = "Optional task description." }, due_date = new { type = "string", description = "Optional ISO-8601 due date." } },
                required = new[] { "board_id", "status_id", "title" }, additionalProperties = false
            }, false),
            Tool("update_task_status", "Move a visible task to a status in the same workspace.", new
            {
                type = "object",
                properties = new { task_id = new { type = "string", description = "The FlowCore task UUID." }, status_id = new { type = "string", description = "The target status UUID." } },
                required = new[] { "task_id", "status_id" }, additionalProperties = false
            }, false),
            Tool("assign_task_users", "Assign the connected user and/or named workspace members to a visible task.", new
            {
                type = "object",
                properties = new { task_id = new { type = "string", description = "The FlowCore task UUID." }, assignees = new { type = "array", description = "Names of users to assign. Use 'me' for the connected FlowCore user.", items = new { type = "string" } } },
                required = new[] { "task_id", "assignees" }, additionalProperties = false
            }, false)
        }
    };

    private static object Tool(string name, string description, object inputSchema, bool readOnly) => new
    {
        name, description, inputSchema,
        annotations = new { readOnlyHint = readOnly, destructiveHint = false, idempotentHint = readOnly }
    };

    private static object IdSchema(string name, string description) => new
    {
        type = "object",
        properties = new Dictionary<string, object> { [name] = new { type = "string", description } },
        required = new[] { name }, additionalProperties = false
    };
}
