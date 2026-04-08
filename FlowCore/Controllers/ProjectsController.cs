using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class ProjectsController : BaseController
{
    private readonly IProjectRepository _projects;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public ProjectsController(IProjectRepository projects, IBreadcrumbTrailBuilder breadcrumbs)
    {
        _projects = projects;
        _breadcrumbs = breadcrumbs;
    }

    public IActionResult Index(Guid? workspaceId)
    {
        var list = workspaceId is null
            ? _projects.GetAll()
            : _projects.GetByWorkspaceId(workspaceId.Value);
        var rows = list
            .Select(p => new ProjectListRow(p.Id, p.Key, p.Name, p.WorkspaceId, p.Status))
            .ToList();
        ViewBag.FilterWorkspaceId = workspaceId;
        if (workspaceId is { } w)
            SetNav(w);
        return View(rows);
    }

    public IActionResult Details(Guid id)
    {
        var entity = _projects.GetById(id);
        if (entity is not null)
            SetNav(entity.WorkspaceId, entity.Id);
        return ViewDetails(entity, _breadcrumbs.ForProject);
    }
}
