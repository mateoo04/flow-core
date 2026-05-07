using System.Diagnostics;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class HomeController : Controller
{
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;

    public HomeController(ITaskRepository tasks, IUserRepository users)
    {
        _tasks = tasks;
        _users = users;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var currentUserId = Data.DemoSeedIds.UserAlex;
        var user = await _users.GetByIdAsync(currentUserId, ct);

        var tasks = await _tasks.GetAssignedToUserAsync(currentUserId, ct);

        var groups = tasks
            .GroupBy(t => t.TaskStatusDefinition?.Name ?? "Unknown")
            .Select(g =>
            {
                var sortKey = g.Min(t => t.TaskStatusDefinition?.Position ?? 999);
                var color = g.Select(t => t.TaskStatusDefinition?.ColorHex).FirstOrDefault(c => !string.IsNullOrEmpty(c));
                var cards = g
                    .OrderBy(t => t.Title)
                    .Select(t =>
                    {
                        var project = t.Board?.Project;
                        return new MyTaskCardVm
                        {
                            TaskId = t.Id,
                            Title = t.Title,
                            ProjectId = project?.Id ?? Guid.Empty,
                            ProjectName = project?.Name ?? "Project",
                            Assignees = TaskAssigneeStackBuilder.FromTask(t)
                        };
                    })
                    .ToList();

                return new StatusTaskGroupVm
                {
                    StatusName = g.Key,
                    StatusColorHex = color,
                    SortKey = sortKey,
                    Tasks = cards
                };
            })
            .OrderBy(x => x.SortKey)
            .ThenBy(x => x.StatusName)
            .ToList();

        var vm = new MyWorkViewModel
        {
            CurrentUserDisplayName = user?.FullName ?? "You",
            StatusGroups = groups
        };
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
