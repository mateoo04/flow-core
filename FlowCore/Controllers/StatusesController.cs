using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class StatusesController : BaseController
{
    private readonly IStatusRepository _repo;

    public StatusesController(IStatusRepository repo) => _repo = repo;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var statuses = await _repo.GetAllAsync(ct);
        var rows = statuses
            .Select(s => new StatusListRow(
                s.Id,
                s.Name,
                s.ColorHex,
                s.Position,
                s.IsDoneState,
                s.WorkspaceId,
                s.Workspace?.Name ?? "(no workspace)"))
            .ToList();
        return View(rows);
    }
}
