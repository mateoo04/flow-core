using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;

namespace FlowCore.Controllers;

public abstract class BaseController : Controller
{
    /// <summary>Highlights workspace/project in the layout sidebar when set.</summary>
    protected void SetNav(Guid? workspaceId, Guid? projectId = null)
    {
        if (workspaceId is { } ws)
            ViewData["ActiveWorkspaceId"] = ws;
        if (projectId is { } p)
            ViewData["ActiveProjectId"] = p;
    }

    /// <summary>Renders Details view or 404; sets <see cref="ViewBag.Breadcrumbs"/> when entity exists.</summary>
    protected IActionResult ViewDetails<T>(T? entity, Func<T, IReadOnlyList<BreadcrumbItem>> breadcrumbTrail) where T : class
    {
        if (entity is null)
            return NotFound();
        ViewBag.Breadcrumbs = breadcrumbTrail(entity);
        return View(entity);
    }
}
