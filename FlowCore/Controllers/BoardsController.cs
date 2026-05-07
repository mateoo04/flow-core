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

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var boards = await _boards.GetAllAsync(ct);
        var rows = boards
            .Select(b => new BoardListRow(b.Id, b.Name, b.ProjectId, b.IsDefault, b.Tasks.Count))
            .ToList();
        return View(rows);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _boards.GetByIdAsync(id, ct);
        if (entity is null)
            return NotFound();

        if (entity.Project is not null)
            SetNav(entity.Project.WorkspaceId, entity.ProjectId);

        return RedirectToAction(nameof(ProjectsController.Details), "Projects",
            new { id = entity.ProjectId, boardId = entity.Id });
    }
}
