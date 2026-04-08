using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class BoardsController : BaseController
{
    private readonly IBoardRepository _boards;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public BoardsController(IBoardRepository boards, IBreadcrumbTrailBuilder breadcrumbs)
    {
        _boards = boards;
        _breadcrumbs = breadcrumbs;
    }

    public IActionResult Index()
    {
        var rows = _boards.GetAll()
            .Select(b => new BoardListRow(b.Id, b.Name, b.ProjectId, b.IsDefault, b.Columns.Count))
            .ToList();
        return View(rows);
    }

    public IActionResult Details(Guid id)
    {
        var entity = _boards.GetById(id);
        return ViewDetails(entity, _breadcrumbs.ForBoard);
    }
}
