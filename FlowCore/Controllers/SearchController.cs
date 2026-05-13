using FlowCore.Data;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers;

public class SearchController : Controller
{
    private const int ResultLimit = 8;

    private readonly IProjectRepository _projects;
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly FlowCoreDbContext _db;

    public SearchController(
        IProjectRepository projects,
        ITaskRepository tasks,
        IUserRepository users,
        ICurrentUserAccessor currentUser,
        FlowCoreDbContext db)
    {
        _projects = projects;
        _tasks = tasks;
        _users = users;
        _currentUser = currentUser;
        _db = db;
    }

    [HttpGet("/search/results")]
    public async Task<IActionResult> Results(
        [FromQuery] string? tab,
        [FromQuery] string? q,
        CancellationToken ct)
    {
        var query = (q ?? string.Empty).Trim();
        var parsedTab = ParseTab(tab);
        if (parsedTab is null) return BadRequest();

        var userWorkspaceIds = await _db.WorkspaceMembers
            .Where(m => m.UserId == _currentUser.UserId)
            .Select(m => m.WorkspaceId)
            .ToListAsync(ct);

        return parsedTab switch
        {
            SearchTab.Projects => await ProjectsAsync(query, userWorkspaceIds, ct),
            SearchTab.Tasks => await TasksAsync(query, userWorkspaceIds, ct),
            SearchTab.Users => await UsersAsync(query, ct),
            _ => BadRequest()
        };
    }

    private async Task<IActionResult> ProjectsAsync(string query, List<Guid> userWorkspaceIds, CancellationToken ct)
    {
        // TODO(auth-followup): membership filter is applied in-memory after over-fetching.
        // For datasets where a user's accessible projects fall outside the first
        // (ResultLimit * 3) alphabetically, this can miss results. Push the workspace
        // filter into the repo query when ProjectRepo exposes a workspace-scoped variant.
        var hits = query.Length == 0
            ? Array.Empty<Models.Project>()
            : (await _projects.SearchAsync(query, ResultLimit * 3, ct))
                .Where(p => userWorkspaceIds.Contains(p.WorkspaceId))
                .Take(ResultLimit)
                .ToArray();

        var rows = hits.Select(p => new SearchProjectRow(p.Id, p.Name, p.Workspace?.Name)).ToList();
        return PartialView("_SearchResultsProjects", new SearchResultsVm<SearchProjectRow>(query, rows));
    }

    private async Task<IActionResult> TasksAsync(string query, List<Guid> userWorkspaceIds, CancellationToken ct)
    {
        // TODO(auth-followup): membership filter is applied in-memory after over-fetching.
        // For datasets where a user's accessible tasks fall outside the first
        // (ResultLimit * 3) alphabetically, this can miss results. Push the workspace
        // filter into the repo query when TaskRepo exposes a workspace-scoped variant.
        var hits = query.Length == 0
            ? Array.Empty<Models.TaskItem>()
            : (await _tasks.SearchAsync(query, ResultLimit * 3, ct))
                .Where(t => t.Board?.Project?.WorkspaceId is { } wsId && userWorkspaceIds.Contains(wsId))
                .Take(ResultLimit)
                .ToArray();

        var rows = hits.Select(t => new SearchTaskRow(
            t.Id,
            t.Title,
            t.Board?.Project?.Name,
            string.IsNullOrWhiteSpace(t.TaskStatusDefinition?.ColorHex) ? null : t.TaskStatusDefinition!.ColorHex)).ToList();
        return PartialView("_SearchResultsTasks", new SearchResultsVm<SearchTaskRow>(query, rows));
    }

    private async Task<IActionResult> UsersAsync(string query, CancellationToken ct)
    {
        var hits = query.Length == 0
            ? Array.Empty<Models.User>()
            : await _users.SearchActiveAsync(query, Array.Empty<Guid>(), ResultLimit, ct);

        var rows = hits.Select(u => new SearchUserRow(
            u.Id,
            u.FullName,
            u.Email,
            UserDisplayHelper.GetInitials(u.FullName),
            UserDisplayHelper.BackgroundColorForUser(u.Id))).ToList();
        return PartialView("_SearchResultsUsers", new SearchResultsVm<SearchUserRow>(query, rows));
    }

    private static SearchTab? ParseTab(string? tab) => tab switch
    {
        "projects" => SearchTab.Projects,
        "tasks" => SearchTab.Tasks,
        "users" => SearchTab.Users,
        _ => null
    };
}
