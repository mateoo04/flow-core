using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class BoardsController : BaseController
{
    private readonly IBoardRepository _boards;

    public BoardsController(IBoardRepository boards)
    {
        _boards = boards;
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
        if (entity is null)
            return NotFound();

        if (entity.Project is not null)
            SetNav(entity.Project.WorkspaceId, entity.ProjectId);

        return RedirectToAction(nameof(ProjectsController.Details), "Projects",
            new { id = entity.ProjectId, boardId = entity.Id });
    }
}
