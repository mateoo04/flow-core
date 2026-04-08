using System.Diagnostics;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
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
        var workspaces = _graph.Workspaces;
        var vm = new DashboardViewModel
        {
            WorkspaceCount = workspaces.Count,
            ProjectCount = workspaces.Sum(w => w.Projects.Count),
            TaskCount = DemoDataLinqExamples.AllTasks(workspaces).Count(),
            UserCount = _graph.Users.Count,
            TagCount = _graph.Tags.Count,
            HotTaskCount = DemoDataLinqExamples.HotTasks(workspaces).Count(),
            TasksByStatus = DemoDataLinqExamples.TaskCountByStatus(workspaces).ToList(),
            TopProjectsByTasks = DemoDataLinqExamples.ProjectsByTaskVolumeWithIds(workspaces).Take(8).ToList()
        };
        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
