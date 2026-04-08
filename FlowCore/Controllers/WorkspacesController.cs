using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class WorkspacesController : BaseController
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public WorkspacesController(IWorkspaceRepository workspaces, IBreadcrumbTrailBuilder breadcrumbs)
    {
        _workspaces = workspaces;
        _breadcrumbs = breadcrumbs;
    }

    public IActionResult Index()
    {
        var rows = _workspaces.GetAll()
            .Select(w => new WorkspaceListRow(w.Id, w.Name, w.Visibility, w.Projects.Count))
            .ToList();
        return View(rows);
    }

    public IActionResult Details(Guid id)
    {
        var entity = _workspaces.GetById(id);
        return ViewDetails(entity, _breadcrumbs.ForWorkspace);
    }
}
