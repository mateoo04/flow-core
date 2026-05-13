using FlowCore.Data;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers;

public class StatusesController : BaseController
{
    private readonly IStatusRepository _repo;
    private readonly FlowCoreDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public StatusesController(IStatusRepository repo, FlowCoreDbContext db, ICurrentUserAccessor currentUser)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userWorkspaceIds = await _db.WorkspaceMembers
            .Where(m => m.UserId == _currentUser.UserId)
            .Select(m => m.WorkspaceId)
            .ToListAsync(ct);

        var statuses = await _repo.GetAllAsync(ct);
        var rows = statuses
            .Where(s => userWorkspaceIds.Contains(s.WorkspaceId))
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
