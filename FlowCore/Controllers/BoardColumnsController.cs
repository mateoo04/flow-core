using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class BoardColumnsController : BaseController
{
    private readonly IBoardColumnRepository _columns;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public BoardColumnsController(IBoardColumnRepository columns, IBreadcrumbTrailBuilder breadcrumbs)
    {
        _columns = columns;
        _breadcrumbs = breadcrumbs;
    }

    public IActionResult Index()
    {
        var rows = _columns.GetAll()
            .Select(c => new BoardColumnListRow(c.Id, c.Name, c.BoardId, c.Position, c.Tasks.Count, c.IsDoneColumn))
            .OrderBy(r => r.BoardId)
            .ThenBy(r => r.Position)
            .ToList();
        return View(rows);
    }

    public IActionResult Details(Guid id)
    {
        var entity = _columns.GetById(id);
        return ViewDetails(entity, _breadcrumbs.ForBoardColumn);
    }
}
