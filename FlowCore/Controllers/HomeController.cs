using System.Diagnostics;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class HomeController : Controller
{
    private readonly DemoDataGraph _graph;

    public HomeController(DemoDataGraph graph)
    {
        _graph = graph;
    }

    public IActionResult Index()
    {
        var currentUserId = DemoSeedIds.UserAlex;
        var user = _graph.Users.FirstOrDefault(u => u.Id == currentUserId);

        var tasks = DemoDataLinqExamples.AllTasks(_graph.Workspaces)
            .Where(t => t.TaskAssignments.Any(a => a.UserId == currentUserId && a.Role == TaskRole.Assignee))
            .ToList();

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
