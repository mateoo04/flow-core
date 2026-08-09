using System.Text.Json;
using FlowCore.Data;
using FlowCore.Mcp.Protocol;
using FlowCore.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Mcp.Services;

internal sealed class FlowCoreMcpService(DbContextOptions<FlowCoreDbContext> dbOptions, string userEmail)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<object> CallToolAsync(JsonElement parameters)
    {
        if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("name", out var nameElement) || string.IsNullOrWhiteSpace(nameElement.GetString()))
            throw new McpException(-32602, "tools/call requires a tool name.");

        var arguments = parameters.TryGetProperty("arguments", out var args) ? args : default;
        await using var db = new FlowCoreDbContext(dbOptions);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == userEmail && u.IsActive);
        if (user is null) throw new McpException(-32001, "The configured FlowCore MCP user was not found or is inactive.");

        object data = nameElement.GetString() switch
        {
            "list_projects" => await ListProjectsAsync(db, user.Id),
            "search_tasks" => await SearchTasksAsync(db, user.Id, OptionalString(arguments, "query")),
            "get_project_board" => await GetProjectBoardAsync(db, user.Id, RequiredGuid(arguments, "project_id")),
            "create_task" => await CreateTaskAsync(db, user.Id, arguments),
            "update_task_status" => await UpdateTaskStatusAsync(db, user.Id, arguments),
            "assign_task_users" => await AssignTaskUsersAsync(db, user, arguments),
            _ => throw new McpException(-32602, "Unknown FlowCore tool.")
        };

        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(data, JsonOptions) } } };
    }

    private static async Task<object> ListProjectsAsync(FlowCoreDbContext db, Guid userId) => await db.Projects.AsNoTracking().Where(p => db.WorkspaceMembers.Any(m => m.WorkspaceId == p.WorkspaceId && m.UserId == userId)).OrderBy(p => p.Name).Select(p => new { p.Id, p.Name, p.Description, p.Status, p.Priority, p.DueDate, Workspace = p.Workspace!.Name }).ToListAsync();

    private static async Task<object> SearchTasksAsync(FlowCoreDbContext db, Guid userId, string? query)
    {
        var tasks = db.TaskItems.AsNoTracking().Where(t => db.WorkspaceMembers.Any(m => m.WorkspaceId == t.Board!.Project!.WorkspaceId && m.UserId == userId));
        if (!string.IsNullOrWhiteSpace(query)) tasks = tasks.Where(t => t.Title.Contains(query) || t.Description.Contains(query));
        return await tasks.OrderByDescending(t => t.UpdatedAt).Take(50).Select(t => new { t.Id, t.Title, t.Description, t.Priority, t.DueDate, Project = t.Board!.Project!.Name, Board = t.Board.Name, Status = t.TaskStatusDefinition!.Name }).ToListAsync();
    }

    private static async Task<object> GetProjectBoardAsync(FlowCoreDbContext db, Guid userId, Guid projectId)
    {
        var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(p => p.Id == projectId);
        if (project is null || !await IsMemberAsync(db, project.WorkspaceId, userId)) throw new McpException(-32004, "Project not found or not accessible to the connected user.");
        var statuses = await db.TaskStatusDefinitions.AsNoTracking().Where(s => s.WorkspaceId == project.WorkspaceId).OrderBy(s => s.Position).Select(s => new { s.Id, s.Name, s.IsDoneState }).ToListAsync();
        var boards = await db.Boards.AsNoTracking().Where(b => b.ProjectId == projectId).OrderBy(b => b.Position).Select(b => new { b.Id, b.Name, b.Position }).ToListAsync();
        var boardIds = boards.Select(b => b.Id).ToArray();
        var tasks = await db.TaskItems.AsNoTracking().Where(t => boardIds.Contains(t.BoardId)).OrderBy(t => t.Position).Select(t => new { t.Id, t.BoardId, t.Title, t.TaskStatusDefinitionId, t.Priority, t.DueDate }).ToListAsync();
        return new { project = new { project.Id, project.Name }, boards, statuses, tasks };
    }

    private static async Task<object> CreateTaskAsync(FlowCoreDbContext db, Guid userId, JsonElement arguments)
    {
        var boardId = RequiredGuid(arguments, "board_id"); var statusId = RequiredGuid(arguments, "status_id"); var title = RequiredString(arguments, "title");
        if (title.Length > 200) throw new McpException(-32602, "title must be 200 characters or fewer.");
        var board = await db.Boards.Include(b => b.Project).SingleOrDefaultAsync(b => b.Id == boardId);
        if (board?.Project is null || !await IsMemberAsync(db, board.Project.WorkspaceId, userId)) throw new McpException(-32004, "Board not found or not accessible to the connected user.");
        var status = await db.TaskStatusDefinitions.SingleOrDefaultAsync(s => s.Id == statusId && s.WorkspaceId == board.Project.WorkspaceId);
        if (status is null) throw new McpException(-32602, "status_id must belong to the board's workspace.");
        var now = DateTime.UtcNow;
        var position = (await db.TaskItems.Where(t => t.BoardId == boardId).Select(t => (int?)t.Position).MaxAsync() ?? -1) + 1;
        var task = new TaskItem { Id = Guid.NewGuid(), BoardId = boardId, TaskStatusDefinitionId = statusId, Title = title, Description = OptionalString(arguments, "description") ?? string.Empty, DueDate = OptionalDateTime(arguments, "due_date"), Priority = TaskPriority.Medium, Position = position, CreatedAt = now, UpdatedAt = now };
        db.TaskItems.Add(task); await db.SaveChangesAsync();
        return new { task.Id, task.Title, task.BoardId, task.TaskStatusDefinitionId, task.DueDate };
    }

    private static async Task<object> UpdateTaskStatusAsync(FlowCoreDbContext db, Guid userId, JsonElement arguments)
    {
        var task = await FindAccessibleTaskAsync(db, userId, RequiredGuid(arguments, "task_id"));
        var statusId = RequiredGuid(arguments, "status_id");
        var status = await db.TaskStatusDefinitions.SingleOrDefaultAsync(s => s.Id == statusId && s.WorkspaceId == task.Board!.Project!.WorkspaceId);
        if (status is null) throw new McpException(-32602, "status_id must belong to the task's workspace.");
        task.TaskStatusDefinitionId = statusId; task.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        return new { task.Id, task.Title, task.TaskStatusDefinitionId, status = status.Name };
    }

    private static async Task<object> AssignTaskUsersAsync(FlowCoreDbContext db, User currentUser, JsonElement arguments)
    {
        var task = await FindAccessibleTaskAsync(db, currentUser.Id, RequiredGuid(arguments, "task_id"));
        var assignees = new List<User>();
        foreach (var name in RequiredStrings(arguments, "assignees").Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(name, "me", StringComparison.OrdinalIgnoreCase)) { assignees.Add(currentUser); continue; }
            var matches = await db.Users.Where(u => EF.Functions.ILike(u.FullName, name)).Where(u => db.WorkspaceMembers.Any(m => m.UserId == u.Id && m.WorkspaceId == task.Board!.Project!.WorkspaceId)).Take(2).ToListAsync();
            if (matches.Count == 0) throw new McpException(-32004, $"No workspace member named '{name}' was found.");
            if (matches.Count > 1) throw new McpException(-32602, $"More than one workspace member is named '{name}'. Use a more specific name.");
            assignees.Add(matches[0]);
        }
        var assigneeIds = assignees.Select(u => u.Id).Distinct().ToArray();
        var existingIds = await db.TaskAssignments.Where(a => a.TaskItemId == task.Id && assigneeIds.Contains(a.UserId)).Select(a => a.UserId).ToListAsync();
        foreach (var assignee in assignees.Where(u => !existingIds.Contains(u.Id))) db.TaskAssignments.Add(new TaskAssignment { TaskItemId = task.Id, UserId = assignee.Id, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return new { task.Id, task.Title, assignees = assignees.Select(u => new { u.Id, u.FullName, u.Email }) };
    }

    private static async Task<TaskItem> FindAccessibleTaskAsync(FlowCoreDbContext db, Guid userId, Guid taskId)
    {
        var task = await db.TaskItems.Include(t => t.Board).ThenInclude(b => b!.Project).SingleOrDefaultAsync(t => t.Id == taskId);
        if (task?.Board?.Project is null || !await IsMemberAsync(db, task.Board.Project.WorkspaceId, userId)) throw new McpException(-32004, "Task not found or not accessible to the connected user.");
        return task;
    }

    private static Task<bool> IsMemberAsync(FlowCoreDbContext db, Guid workspaceId, Guid userId) => db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
    private static Guid RequiredGuid(JsonElement arguments, string property) => Guid.TryParse(RequiredString(arguments, property), out var value) ? value : throw new McpException(-32602, $"{property} must be a UUID.");
    private static string RequiredString(JsonElement arguments, string property) => OptionalString(arguments, property) is { Length: > 0 } value ? value : throw new McpException(-32602, $"{property} is required.");
    private static string? OptionalString(JsonElement arguments, string property) => arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static DateTime? OptionalDateTime(JsonElement arguments, string property) => OptionalString(arguments, property) is { } value ? DateTime.TryParse(value, out var date) ? date : throw new McpException(-32602, $"{property} must be an ISO-8601 date.") : null;
    private static IReadOnlyList<string> RequiredStrings(JsonElement arguments, string property)
    {
        if (arguments.ValueKind != JsonValueKind.Object || !arguments.TryGetProperty(property, out var values) || values.ValueKind != JsonValueKind.Array) throw new McpException(-32602, $"{property} must be a non-empty array of names.");
        var result = values.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()).Where(v => !string.IsNullOrWhiteSpace(v)).Cast<string>().ToList();
        return result.Count > 0 ? result : throw new McpException(-32602, $"{property} must be a non-empty array of names.");
    }
}
