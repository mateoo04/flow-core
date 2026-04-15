using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class SettingsController : Controller
{
    private readonly InMemoryDataStore _store;
    private readonly IWorkspaceRepository _workspaces;

    public SettingsController(InMemoryDataStore store, IWorkspaceRepository workspaces)
    {
        _store = store;
        _workspaces = workspaces;
    }

    public IActionResult Index(Guid? workspaceId)
    {
        Workspace? ws;
        List<TaskStatusDefinition> statuses;
        lock (_store.Sync)
        {
            ws = workspaceId is { } wid
                ? _store.FindWorkspace(wid)
                : _store.Workspaces.FirstOrDefault();
            if (ws is null)
                return NotFound();
            statuses = ws.TaskStatusDefinitions.OrderBy(s => s.Position).ToList();
        }

        ViewData["ActiveWorkspaceId"] = ws.Id;
        var vm = new SettingsIndexVm(ws, statuses, _store.Workspaces.OrderBy(w => w.Name).ToList());
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Guid workspaceId, TaskStatusFormVm model)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["SettingsError"] = "Status name is required.";
            return RedirectToAction(nameof(Index), new { workspaceId });
        }

        lock (_store.Sync)
        {
            var ws = _store.FindWorkspace(workspaceId);
            if (ws is null)
                return NotFound();

            var nextPos = ws.TaskStatusDefinitions.Count == 0
                ? 0
                : ws.TaskStatusDefinitions.Max(s => s.Position) + 1;
            ws.TaskStatusDefinitions.Add(new TaskStatusDefinition
            {
                Id = Guid.NewGuid(),
                WorkspaceId = ws.Id,
                Workspace = ws,
                Name = model.Name.Trim(),
                ColorHex = string.IsNullOrWhiteSpace(model.ColorHex) ? "#94A3B8" : model.ColorHex.Trim(),
                Position = nextPos,
                IsDoneState = model.IsDoneState,
                CreatedAt = DateTime.UtcNow
            });
        }
        return RedirectToAction(nameof(Index), new { workspaceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(Guid workspaceId, Guid id, TaskStatusFormVm model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["SettingsError"] = "Status name is required.";
            return RedirectToAction(nameof(Index), new { workspaceId });
        }

        lock (_store.Sync)
        {
            var ws = _store.FindWorkspace(workspaceId);
            if (ws is null)
                return NotFound();
            var s = ws.TaskStatusDefinitions.FirstOrDefault(x => x.Id == id);
            if (s is null)
                return NotFound();
            s.Name = model.Name.Trim();
            s.ColorHex = string.IsNullOrWhiteSpace(model.ColorHex) ? "#94A3B8" : model.ColorHex.Trim();
            s.IsDoneState = model.IsDoneState;
        }
        return RedirectToAction(nameof(Index), new { workspaceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reorder(Guid workspaceId, Guid id, int direction)
    {
        lock (_store.Sync)
        {
            var ws = _store.FindWorkspace(workspaceId);
            if (ws is null)
                return NotFound();
            var ordered = ws.TaskStatusDefinitions.OrderBy(s => s.Position).ToList();
            var idx = ordered.FindIndex(s => s.Id == id);
            if (idx < 0)
                return NotFound();
            var swap = idx + (direction < 0 ? -1 : 1);
            if (swap < 0 || swap >= ordered.Count)
                return RedirectToAction(nameof(Index), new { workspaceId });

            (ordered[idx].Position, ordered[swap].Position) = (ordered[swap].Position, ordered[idx].Position);
        }
        return RedirectToAction(nameof(Index), new { workspaceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid workspaceId, Guid id)
    {
        lock (_store.Sync)
        {
            var ws = _store.FindWorkspace(workspaceId);
            if (ws is null)
                return NotFound();
            var s = ws.TaskStatusDefinitions.FirstOrDefault(x => x.Id == id);
            if (s is null)
                return NotFound();

            if (ws.TaskStatusDefinitions.Count <= 1)
            {
                TempData["SettingsError"] = "At least one status must remain.";
                return RedirectToAction(nameof(Index), new { workspaceId });
            }

            if (s.TaskItems.Count > 0)
            {
                TempData["SettingsError"] = $"Cannot delete \"{s.Name}\" — {s.TaskItems.Count} task(s) still use it. Reassign them first.";
                return RedirectToAction(nameof(Index), new { workspaceId });
            }

            ws.TaskStatusDefinitions.Remove(s);
        }
        return RedirectToAction(nameof(Index), new { workspaceId });
    }
}
