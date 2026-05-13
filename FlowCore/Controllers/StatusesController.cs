using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers;

public class StatusesController : BaseController
{
    private readonly IStatusRepository _repo;
    private readonly FlowCoreDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IWorkspaceRepository _workspaces;
    private readonly IAuthorizationService _authz;

    public StatusesController(
        IStatusRepository repo,
        FlowCoreDbContext db,
        ICurrentUserAccessor currentUser,
        IWorkspaceRepository workspaces,
        IAuthorizationService authz)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _workspaces = workspaces;
        _authz = authz;
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

    [HttpGet("/workspaces/{workspaceId:guid}/statuses", Name = "workspace-status-settings")]
    public async Task<IActionResult> Manage(Guid workspaceId, CancellationToken ct)
    {
        if (await EnsureWorkspaceMemberAsync(workspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        var ws = await _db.Workspaces
            .AsNoTracking()
            .Include(w => w.TaskStatusDefinitions)
                .ThenInclude(s => s.TaskItems)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (ws is null)
            return NotFound();

        var statuses = ws.TaskStatusDefinitions.OrderBy(s => s.Position).ToList();
        var allWorkspaces = await _workspaces.GetForUserAsync(_currentUser.UserId, ct);

        ViewData["ActiveWorkspaceId"] = ws.Id;
        return View(new WorkspaceStatusSettingsVm(ws, statuses, allWorkspaces.OrderBy(w => w.Name).ToList()));
    }

    [HttpPost("/workspaces/{workspaceId:guid}/statuses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid workspaceId, TaskStatusFormVm model, CancellationToken ct)
    {
        if (await EnsureWorkspaceMemberAsync(workspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["StatusSettingsError"] = "Status name is required.";
            return RedirectToAction(nameof(Manage), new { workspaceId });
        }

        var ws = await _db.Workspaces
            .Include(w => w.TaskStatusDefinitions)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);
        if (ws is null)
            return NotFound();

        var nextPos = ws.TaskStatusDefinitions.Count == 0
            ? 0
            : ws.TaskStatusDefinitions.Max(s => s.Position) + 1;
        _db.TaskStatusDefinitions.Add(new TaskStatusDefinition
        {
            Id = Guid.NewGuid(),
            WorkspaceId = ws.Id,
            Name = model.Name.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(model.ColorHex) ? "#94A3B8" : model.ColorHex.Trim(),
            Position = nextPos,
            IsDoneState = model.IsDoneState,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Manage), new { workspaceId });
    }

    [HttpPost("/workspaces/{workspaceId:guid}/statuses/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid workspaceId, Guid id, TaskStatusFormVm model, CancellationToken ct)
    {
        if (await EnsureWorkspaceMemberAsync(workspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["StatusSettingsError"] = "Status name is required.";
            return RedirectToAction(nameof(Manage), new { workspaceId });
        }

        var s = await _db.TaskStatusDefinitions
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.Id == id, ct);
        if (s is null)
            return NotFound();
        s.Name = model.Name.Trim();
        s.ColorHex = string.IsNullOrWhiteSpace(model.ColorHex) ? "#94A3B8" : model.ColorHex.Trim();
        s.IsDoneState = model.IsDoneState;
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Manage), new { workspaceId });
    }

    [HttpPost("/workspaces/{workspaceId:guid}/statuses/{id:guid}/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(Guid workspaceId, Guid id, int direction, CancellationToken ct)
    {
        if (await EnsureWorkspaceMemberAsync(workspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        var ordered = await _db.TaskStatusDefinitions
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderBy(s => s.Position)
            .ToListAsync(ct);
        var idx = ordered.FindIndex(s => s.Id == id);
        if (idx < 0)
            return NotFound();
        var swap = idx + (direction < 0 ? -1 : 1);
        if (swap < 0 || swap >= ordered.Count)
            return RedirectToAction(nameof(Manage), new { workspaceId });

        (ordered[idx].Position, ordered[swap].Position) = (ordered[swap].Position, ordered[idx].Position);
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Manage), new { workspaceId });
    }

    [HttpPost("/workspaces/{workspaceId:guid}/statuses/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid id, CancellationToken ct)
    {
        if (await EnsureWorkspaceMemberAsync(workspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        var statuses = await _db.TaskStatusDefinitions
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderBy(x => x.Position)
            .ToListAsync(ct);
        var s = statuses.FirstOrDefault(x => x.Id == id);
        if (s is null)
            return NotFound();

        if (statuses.Count <= 1)
        {
            TempData["StatusSettingsError"] = "At least one status must remain.";
            return RedirectToAction(nameof(Manage), new { workspaceId });
        }

        var usedCount = await _db.TaskItems.CountAsync(t => t.TaskStatusDefinitionId == id, ct);
        if (usedCount > 0)
        {
            TempData["StatusSettingsError"] = $"Cannot delete \"{s.Name}\" — {usedCount} task(s) still use it. Reassign them first.";
            return RedirectToAction(nameof(Manage), new { workspaceId });
        }

        _db.TaskStatusDefinitions.Remove(s);
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Manage), new { workspaceId });
    }
}
