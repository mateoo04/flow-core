using Microsoft.AspNetCore.Mvc;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class WorkspacesController : BaseController
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public WorkspacesController(
        IWorkspaceRepository workspaces,
        IBreadcrumbTrailBuilder breadcrumbs)
    {
        _workspaces = workspaces;
        _breadcrumbs = breadcrumbs;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var workspaces = await _workspaces.GetAllAsync(ct);
        var rows = workspaces
            .Select(w => new WorkspaceListRow(w.Id, w.Name, w.Visibility, w.Projects.Count))
            .ToList();
        return View(rows);
    }

    [HttpGet("/workspaces/{id:guid}", Name = "workspace-details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _workspaces.GetByIdAsync(id, ct);
        if (entity is not null)
            SetNav(entity.Id);
        return ViewDetails(entity, _breadcrumbs.ForWorkspace);
    }

    [HttpGet("/workspaces/create", Name = "workspace-create-form")]
    public IActionResult Create() => View(new WorkspaceFormVm());

    [HttpPost("/workspaces/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkspaceFormVm model, CancellationToken ct)
    {
        await ValidateAsync(model, excludeId: null, ct);
        if (!ModelState.IsValid)
            return View(model);

        // TODO: replace with authenticated current user when auth is implemented
        var creatorId = DemoSeedIds.UserAlex;

        var ws = await _workspaces.AddAsync(new Workspace
        {
            Id = Guid.NewGuid(),
            Name = model.Name.Trim(),
            Description = model.Description?.Trim() ?? "",
            Visibility = model.Visibility,
            OwnerUserId = creatorId,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return RedirectToAction(nameof(Details), new { id = ws.Id });
    }

    [HttpGet("/workspaces/{id:guid}/edit", Name = "workspace-edit-form")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var entity = await _workspaces.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        SetNav(entity.Id);
        return View(new WorkspaceFormVm
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Visibility = entity.Visibility
        });
    }

    [HttpPost("/workspaces/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, WorkspaceFormVm model, CancellationToken ct)
    {
        model.Id = id;
        await ValidateAsync(model, excludeId: id, ct);
        if (!ModelState.IsValid)
            return View(model);

        var updated = await _workspaces.UpdateAsync(
            id,
            model.Name.Trim(),
            model.Description?.Trim() ?? "",
            model.Visibility,
            ct);
        if (updated is null) return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("/workspaces/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (await _workspaces.HasProjectsAsync(id, ct))
        {
            TempData["WorkspaceError"] = "Move or delete the projects in this workspace before deleting it.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!await _workspaces.TryDeleteAsync(id, ct))
            return NotFound();

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(WorkspaceFormVm model, Guid? excludeId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(model.Name)
            && await _workspaces.NameExistsAsync(model.Name, excludeId, ct))
            ModelState.AddModelError(nameof(WorkspaceFormVm.Name), "A workspace with this name already exists.");
    }
}
