# FlowCore MCP server

`FlowCore.Mcp` exposes a local, STDIO [Model Context Protocol](https://modelcontextprotocol.io/) server for an agentic IDE.
It uses the existing FlowCore database and enforces workspace membership before returning or changing data.

## Identity for the local demo

The server represents one FlowCore account, selected by `FLOWCORE_MCP_USER_EMAIL`. This is intentional for the local demo: no password is stored in the MCP configuration and every query is scoped to that account's workspace memberships.

Set the variables in the local `.env` file (or in your terminal before starting the IDE), for example:

```powershell
$env:ConnectionStrings__FlowCoreDbContext = 'Host=localhost;Port=5432;Database=FlowCore;Username=postgres;Password=postgres'
$env:FLOWCORE_MCP_USER_EMAIL = 'alex@flowcore.demo'
```

For a deployed version, replace this local identity binding with OAuth or a short-lived user access token. Do not expose this server publicly while it has direct database access.

## Add it to Codex

Create `.codex/config.toml` locally (it is intentionally gitignored) and restart the Codex IDE extension:

```toml
[mcp_servers.flowcore]
command = "dotnet"
args = ["run", "--no-build", "--configuration", "Release", "--project", "FlowCore.Mcp/FlowCore.Mcp.csproj", "--no-launch-profile"]
cwd = "C:/path/to/flow-core"
env_vars = ["ConnectionStrings__FlowCoreDbContext", "DATABASE_URL", "FLOWCORE_MCP_USER_EMAIL"]
default_tools_approval_mode = "writes"
enabled = true
```

The `writes` approval mode means read-only tools can run normally while `create_task` and `update_task_status` require a confirmation. In the Codex extension, open the gear menu, select **MCP servers**, add the server, and restart the extension.
Run `dotnet build FlowCore.Mcp/FlowCore.Mcp.csproj -c Release` once before connecting; `--no-build` is important because MCP reserves standard output for protocol messages.

## Available tools

- `list_projects`
- `search_tasks`
- `get_project_board`
- `create_task` (confirmation required)
- `update_task_status` (confirmation required)
- `assign_task_users` (confirmation required; use `me` for the connected account)

## Demo prompts

1. `List my FlowCore projects and their due dates.`
2. `Search my tasks for authentication and show the project and status.`
3. `Show the board for project <project UUID>.`
4. `Create a task titled "Review MCP demo" on board <board UUID>, in status <status UUID>.`
