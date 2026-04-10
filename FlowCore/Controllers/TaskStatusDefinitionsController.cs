using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class TaskStatusDefinitionsController : BaseController
{
    private readonly ITaskStatusDefinitionRepository _statuses;
    private readonly IProjectRepository _projects;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public TaskStatusDefinitionsController(
        ITaskStatusDefinitionRepository statuses,
        IProjectRepository projects,
        IBreadcrumbTrailBuilder breadcrumbs)
    {
        _statuses = statuses;
        _projects = projects;
        _breadcrumbs = breadcrumbs;
    }

    public IActionResult Index()
    {
        var rows = _statuses.GetAll()
            .Select(s => new TaskStatusDefinitionListRow(
                s.Id, s.Name, s.ProjectId, s.ColorHex, s.Position, s.IsDoneState))
            .OrderBy(r => r.ProjectId)
            .ThenBy(r => r.Position)
            .ToList();
        return View(rows);
    }

    public IActionResult Details(Guid id)
    {
        var entity = _statuses.GetById(id);
        if (entity?.Project is not null)
            SetNav(entity.Project.WorkspaceId, entity.ProjectId);
        return ViewDetails(entity, _breadcrumbs.ForTaskStatusDefinition);
    }

    [HttpGet]
    public IActionResult Manage(Guid projectId)
    {
        var project = _projects.GetById(projectId);
        if (project is null)
            return NotFound();

        SetNav(project.WorkspaceId, project.Id);
        ViewBag.ProjectId = projectId;
        ViewBag.Breadcrumbs = _breadcrumbs.ForProject(project);
        var list = _statuses.GetByProjectId(projectId);
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateStatus(Guid projectId, TaskStatusFormVm model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Manage), new { projectId });

        _statuses.Add(projectId, model.Name, model.ColorHex, model.IsDefault, model.IsDoneState);
        return RedirectToAction(nameof(Manage), new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteStatus(Guid id, Guid projectId, Guid? reassignToStatusId)
    {
        _statuses.TryDelete(id, reassignToStatusId);
        return RedirectToAction(nameof(Manage), new { projectId });
    }
}
