using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class TagsController : BaseController
{
    private readonly ITagRepository _tags;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public TagsController(ITagRepository tags, IBreadcrumbTrailBuilder breadcrumbs)
    {
        _tags = tags;
        _breadcrumbs = breadcrumbs;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var tags = await _tags.GetAllAsync(ct);
        var rows = tags
            .Select(t => new TagListRow(t.Id, t.Name, t.ColorHex))
            .ToList();
        return View(rows);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _tags.GetByIdAsync(id, ct);
        return ViewDetails(entity, _breadcrumbs.ForTag);
    }
}
