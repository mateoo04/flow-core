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

    public IActionResult Index()
    {
        var rows = _tags.GetAll()
            .Select(t => new TagListRow(t.Id, t.Name, t.ColorHex))
            .ToList();
        return View(rows);
    }

    public IActionResult Details(Guid id)
    {
        var entity = _tags.GetById(id);
        return ViewDetails(entity, _breadcrumbs.ForTag);
    }
}
