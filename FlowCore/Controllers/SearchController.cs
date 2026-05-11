using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class SearchController : Controller
{
    private const int ResultLimit = 8;

    private readonly IProjectRepository _projects;
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;

    public SearchController(
        IProjectRepository projects,
        ITaskRepository tasks,
        IUserRepository users)
    {
        _projects = projects;
        _tasks = tasks;
        _users = users;
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

        return parsedTab switch
        {
            SearchTab.Projects => await ProjectsAsync(query, ct),
            SearchTab.Tasks => await TasksAsync(query, ct),
            SearchTab.Users => await UsersAsync(query, ct),
            _ => BadRequest()
        };
    }

    private async Task<IActionResult> ProjectsAsync(string query, CancellationToken ct)
    {
        var hits = query.Length == 0
            ? Array.Empty<Models.Project>()
            : await _projects.SearchAsync(query, ResultLimit, ct);

        var rows = hits.Select(p => new SearchProjectRow(p.Id, p.Name, p.Workspace?.Name)).ToList();
        return PartialView("_SearchResultsProjects", new SearchResultsVm<SearchProjectRow>(query, rows));
    }

    private async Task<IActionResult> TasksAsync(string query, CancellationToken ct)
    {
        var hits = query.Length == 0
            ? Array.Empty<Models.TaskItem>()
            : await _tasks.SearchAsync(query, ResultLimit, ct);

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
