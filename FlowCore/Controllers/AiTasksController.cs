using System.Globalization;
using FlowCore.Models;
using FlowCore.Repositories;
using FlowCore.Services.Ai;
using FlowCore.Services.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FlowCore.Controllers;

[Authorize]
public sealed class AiTasksController : BaseController
{
    private readonly IAiTaskExtractionService _extractor;
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly IAuthorizationService _authorization;
    private readonly ITaskService _tasks;

    public AiTasksController(
        IAiTaskExtractionService extractor,
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        IAuthorizationService authorization,
        ITaskService tasks)
    {
        _extractor = extractor;
        _projects = projects;
        _workspaces = workspaces;
        _authorization = authorization;
        _tasks = tasks;
    }

    [HttpPost("/ai/tasks/extract")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Extract([FromForm] AiPromptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Length > 2_000)
            return BadRequest(new { message = "Enter a task description up to 2,000 characters." });
        if (await AuthorizeProjectAsync(request.ProjectId, ct) is { } denied) return denied;

        try
        {
            var draft = await _extractor.ExtractAsync(request.Prompt.Trim(), ct);
            return Json(new { draft.Title, draft.Description, Priority = draft.Priority.ToString().ToLowerInvariant(), DueDate = draft.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
        }
        catch (AiTaskExtractionConfigurationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "AI is not configured yet. Add OPENAI_API_KEY to your local .env file and restart the app." });
        }
        catch (AiTaskExtractionException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "AI could not create a task suggestion. Please try again." });
        }
    }

    [HttpPost("/ai/tasks/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] AiCreateTaskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200)
            return BadRequest(new { message = "The generated task title is invalid." });
        if (request.Description?.Length > 4_000)
            return BadRequest(new { message = "The generated task description is too long." });
        var project = await _projects.GetByIdAsync(request.ProjectId, ct);
        if (project is null) return NotFound();
        if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authorization, ct) is { } denied) return denied;

        var board = project.Boards.OrderBy(b => b.Position).FirstOrDefault(b => b.IsDefault)
                    ?? project.Boards.OrderBy(b => b.Position).FirstOrDefault();
        var status = project.Workspace?.TaskStatusDefinitions.OrderBy(s => s.Position).FirstOrDefault();
        if (board is null || status is null)
            return BadRequest(new { message = "This project does not have a board or task status configured." });

        DateTime? dueDate = null;
        if (!string.IsNullOrWhiteSpace(request.DueDate))
        {
            if (!DateOnly.TryParseExact(request.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                return BadRequest(new { message = "The generated due date is invalid." });
            dueDate = parsedDate.ToDateTime(TimeOnly.MinValue);
        }

        var priority = request.Priority?.ToLowerInvariant() switch
        {
            "low" => TaskPriority.Low,
            "high" => TaskPriority.High,
            _ => TaskPriority.Medium
        };
        var result = await _tasks.CreateAsync(new CreateTaskRequest(board.Id, status.Id, request.Title.Trim(), request.Description?.Trim(), priority, 0, null, dueDate, Array.Empty<Guid>(), null), ct);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error?.Message ?? "Could not create the task." });

        return Json(new { redirectUrl = Url.Action("Details", "Tasks", new { id = result.Value!.Id }) });
    }

    private async Task<IActionResult?> AuthorizeProjectAsync(Guid projectId, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project is null) return NotFound();
        return await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authorization, ct);
    }

    public sealed class AiPromptRequest
    {
        public Guid ProjectId { get; init; }
        public string Prompt { get; init; } = "";
    }

    public sealed class AiCreateTaskRequest
    {
        public Guid ProjectId { get; init; }
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string? Priority { get; init; }
        public string? DueDate { get; init; }
    }
}
