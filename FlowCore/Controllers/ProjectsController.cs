using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class ProjectsController : BaseController
{
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public ProjectsController(
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        IBreadcrumbTrailBuilder breadcrumbs)
    {
        _projects = projects;
        _workspaces = workspaces;
        _breadcrumbs = breadcrumbs;
    }

    public IActionResult Index(Guid? workspaceId)
    {
        var list = workspaceId is null
            ? _projects.GetAll()
            : _projects.GetByWorkspaceId(workspaceId.Value);
        var rows = list
            .Select(p => new ProjectListRow(p.Id, p.Name, p.WorkspaceId, p.Status))
            .ToList();
        ViewBag.FilterWorkspaceId = workspaceId;
        if (workspaceId is { } w)
            SetNav(w);
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create(Guid? workspaceId)
    {
        var workspaces = _workspaces.GetAll();
        if (workspaces.Count == 0)
            return NotFound();

        var vm = new ProjectCreateFormVm
        {
            WorkspaceId = workspaceId ?? workspaces[0].Id,
            Status = ProjectStatus.Planning,
            Priority = ProjectPriority.Medium
        };
        ViewBag.Workspaces = workspaces;
        if (workspaceId is { } w)
            SetNav(w);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ProjectCreateFormVm model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Workspaces = _workspaces.GetAll();
            return View(model);
        }

        try
        {
            var project = _projects.CreateInWorkspace(
                model.WorkspaceId,
                model.Name,
                model.Description,
                model.Status,
                model.Priority);
            return RedirectToAction(nameof(Details), new { id = project.Id });
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, "Could not create project.");
            ViewBag.Workspaces = _workspaces.GetAll();
            return View(model);
        }
    }

    public IActionResult Details(Guid id, Guid? boardId)
    {
        var entity = _projects.GetById(id);
        if (entity is null)
            return NotFound();

        SetNav(entity.WorkspaceId, entity.Id);

        var boards = entity.Boards.OrderBy(b => b.Position).ThenBy(b => b.Name).ToList();
        Board? active = null;
        if (boardId is { } bid)
            active = boards.FirstOrDefault(b => b.Id == bid);
        active ??= boards.FirstOrDefault(b => b.IsDefault);
        active ??= boards.FirstOrDefault();

        var vm = new ProjectDetailsPageViewModel
        {
            Project = entity,
            ActiveBoard = active,
            BoardsOrdered = boards
        };
        ViewBag.Breadcrumbs = _breadcrumbs.ForProject(entity);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        if (!_projects.TryDelete(id))
            return NotFound();
        return RedirectToAction(nameof(Index));
    }
}
