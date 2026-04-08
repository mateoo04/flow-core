using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class TasksController : BaseController
{
    private readonly ITaskRepository _tasks;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public TasksController(ITaskRepository tasks, IBreadcrumbTrailBuilder breadcrumbs)
    {
        _tasks = tasks;
        _breadcrumbs = breadcrumbs;
    }

    public IActionResult Index()
    {
        var rows = _tasks.GetAll()
            .Select(t => new TaskListRow(t.Id, t.Title, t.Priority, t.BoardColumnId, t.ParentTaskItemId))
            .OrderBy(r => r.Title)
            .ToList();
        return View(rows);
    }

    public IActionResult Details(Guid id)
    {
        var entity = _tasks.GetById(id);
        var project = entity?.BoardColumn?.Board?.Project;
        if (project is not null)
            SetNav(project.WorkspaceId, project.Id);
        return ViewDetails(entity, _breadcrumbs.ForTask);
    }
}
