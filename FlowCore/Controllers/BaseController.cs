using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;

namespace FlowCore.Controllers;

public abstract class BaseController : Controller
{
    /// <summary>Renders Details view or 404; sets <see cref="ViewBag.Breadcrumbs"/> when entity exists.</summary>
    protected IActionResult ViewDetails<T>(T? entity, Func<T, IReadOnlyList<BreadcrumbItem>> breadcrumbTrail) where T : class
    {
        if (entity is null)
            return NotFound();
        ViewBag.Breadcrumbs = breadcrumbTrail(entity);
        return View(entity);
    }
}
