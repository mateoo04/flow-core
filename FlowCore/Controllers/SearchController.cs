using FlowCore.Data;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers;

public class SearchController : Controller
{
    private const int ResultLimit = 3;

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
        [FromQuery] string? q,
        [FromQuery] string? section,
        CancellationToken ct,
        [FromQuery] int page = 1)
    {
        var query = (q ?? string.Empty).Trim();
        if (query.Length == 0) return Content(string.Empty);
        if (page < 1) return BadRequest();

        var userWorkspaceIds = await _db.WorkspaceMembers
            .Where(m => m.UserId == _currentUser.UserId)
            .Select(m => m.WorkspaceId)
            .ToListAsync(ct);

        if (section is not null)
        {
            var loadedSection = await GetSectionAsync(section, query, page, userWorkspaceIds, ct);
            return loadedSection is null
                ? BadRequest()
                : PartialView("_GlobalSearchSection", loadedSection);
        }

        var sections = new List<GlobalSearchSectionVm>();
        var pages = GetMatchingPages(query);
        if (pages.Rows.Count > 0) sections.Add(pages);

        foreach (var key in new[] { "tasks", "projects", "users", "comments" })
        {
            var searchSection = await GetSectionAsync(key, query, 1, userWorkspaceIds, ct);
            if (searchSection is { Rows.Count: > 0 }) sections.Add(searchSection);
        }

        return PartialView("_GlobalSearchResults", new GlobalSearchResultsVm(query, sections));
    }

    private async Task<GlobalSearchSectionVm?> GetSectionAsync(
        string section, string query, int page, List<Guid> userWorkspaceIds, CancellationToken ct) => section switch
    {
        "tasks" => await TasksAsync(query, page, userWorkspaceIds, ct),
        "projects" => await ProjectsAsync(query, page, userWorkspaceIds, ct),
        "users" => await UsersAsync(query, page, ct),
        "comments" => await CommentsAsync(query, page, userWorkspaceIds, ct),
        _ => null
    };

    private async Task<GlobalSearchSectionVm> ProjectsAsync(string query, int page, List<Guid> userWorkspaceIds, CancellationToken ct)
    {
        // TODO(auth-followup): membership filter is applied in-memory after over-fetching.
        // For datasets where a user's accessible projects fall outside the first
        // (ResultLimit * 3) alphabetically, this can miss results. Push the workspace
        // filter into the repo query when ProjectRepo exposes a workspace-scoped variant.
        var hits = query.Length == 0
            ? Array.Empty<Models.Project>()
            : (await _projects.SearchAsync(query, (page * ResultLimit + 1) * 3, ct))
                .Where(p => userWorkspaceIds.Contains(p.WorkspaceId))
                .ToArray();

        return BuildSection("projects", "Projects", hits, page, p => new GlobalSearchRow(
            p.Name, p.Workspace?.Name, Url.Action("Details", "Projects", new { id = p.Id })!, "Project"));
    }

    private async Task<GlobalSearchSectionVm> TasksAsync(string query, int page, List<Guid> userWorkspaceIds, CancellationToken ct)
    {
        // TODO(auth-followup): membership filter is applied in-memory after over-fetching.
        // For datasets where a user's accessible tasks fall outside the first
        // (ResultLimit * 3) alphabetically, this can miss results. Push the workspace
        // filter into the repo query when TaskRepo exposes a workspace-scoped variant.
        var hits = query.Length == 0
            ? Array.Empty<Models.TaskItem>()
            : (await _tasks.SearchAsync(query, (page * ResultLimit + 1) * 3, ct))
                .Where(t => t.Board?.Project?.WorkspaceId is { } wsId && userWorkspaceIds.Contains(wsId))
                .ToArray();

        return BuildSection("tasks", "Tasks", hits, page, t => new GlobalSearchRow(
            t.Title, t.Board?.Project?.Name, Url.Action("Details", "Tasks", new { id = t.Id })!, "Task",
            StatusColorHex: string.IsNullOrWhiteSpace(t.TaskStatusDefinition?.ColorHex) ? null : t.TaskStatusDefinition!.ColorHex));
    }

    private async Task<GlobalSearchSectionVm> UsersAsync(string query, int page, CancellationToken ct)
    {
        var hits = query.Length == 0
            ? Array.Empty<Models.User>()
            : await _users.SearchActiveAsync(query, Array.Empty<Guid>(), page * ResultLimit + 1, ct);

        return BuildSection("users", "People", hits, page, u => new GlobalSearchRow(
            u.FullName, u.Email, Url.Action("Details", "Users", new { id = u.Id })!, "Person",
            UserDisplayHelper.GetInitials(u.FullName), UserDisplayHelper.BackgroundColorForUser(u.Id)));
    }

    private async Task<GlobalSearchSectionVm> CommentsAsync(
        string query, int page, List<Guid> userWorkspaceIds, CancellationToken ct)
    {
        var pattern = $"%{query}%";
        var hits = await _db.Comments
            .AsNoTracking()
            .Include(c => c.Author)
            .Include(c => c.TaskItem)
                .ThenInclude(t => t!.Board)
                    .ThenInclude(b => b!.Project)
            .Where(c => EF.Functions.ILike(c.Body, pattern))
            .Where(c => userWorkspaceIds.Contains(c.TaskItem!.Board!.Project!.WorkspaceId))
            .OrderByDescending(c => c.CreatedAt)
            .Take(page * ResultLimit + 1)
            .ToListAsync(ct);

        return BuildSection("comments", "Comments", hits, page, c => new GlobalSearchRow(
            CommentExcerpt(c.Body),
            $"{c.TaskItem!.Title} · {c.Author?.FullName ?? "User"}",
            $"{Url.Action("Details", "Tasks", new { id = c.TaskItemId })}#comment-{c.Id}",
            "Comment"));
    }

    private GlobalSearchSectionVm GetMatchingPages(string query)
    {
        var pages = new List<GlobalSearchRow>
        {
            new("My tasks", "Page", Url.Action("Index", "Home")!, "Page"),
            new("Workspaces", "Page", Url.Action("Index", "Workspaces")!, "Page"),
            new("Projects", "Page", Url.Action("Index", "Projects")!, "Page"),
            new("New project", "Page", Url.Action("Create", "Projects")!, "Page"),
            new("Settings", "Page", Url.Action("Index", "Settings")!, "Page")
        };

        if (User.IsInRole(Models.AppRoles.Admin))
            pages.Add(new GlobalSearchRow("Admin", "Page", Url.Action("Index", "Admin")!, "Page"));

        var matches = pages.Where(p => p.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        return new GlobalSearchSectionVm("pages", "Pages", matches, false, 0);
    }

    private static GlobalSearchSectionVm BuildSection<TEntity>(
        string key, string title, IReadOnlyList<TEntity> hits, int page, Func<TEntity, GlobalSearchRow> map)
    {
        var skipped = (page - 1) * ResultLimit;
        var rows = hits.Skip(skipped).Take(ResultLimit).Select(map).ToList();
        return new GlobalSearchSectionVm(key, title, rows, hits.Count > skipped + rows.Count, page + 1);
    }

    private static string CommentExcerpt(string body)
    {
        var compact = string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= 120 ? compact : $"{compact[..117]}…";
    }
}
