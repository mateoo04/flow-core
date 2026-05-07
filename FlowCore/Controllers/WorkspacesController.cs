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

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var workspaces = await _workspaces.GetAllAsync(ct);
        var rows = workspaces
            .Select(w => new WorkspaceListRow(w.Id, w.Name, w.Visibility, w.Projects.Count))
            .ToList();
        return View(rows);
    }

    [HttpGet("/workspaces/{id:guid}", Name = "workspace-details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _workspaces.GetByIdAsync(id, ct);
        if (entity is not null)
            SetNav(entity.Id);
        return ViewDetails(entity, _breadcrumbs.ForWorkspace);
    }
}
