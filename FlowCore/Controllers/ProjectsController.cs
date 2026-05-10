using FlowCore.Common;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using FlowCore.Services.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FlowCore.Controllers;

public class ProjectsController : BaseController
{
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly IProjectService _projectService;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public ProjectsController(
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        IProjectService projectService,
        IBreadcrumbTrailBuilder breadcrumbs)
    {
        _projects = projects;
        _workspaces = workspaces;
        _projectService = projectService;
        _breadcrumbs = breadcrumbs;
    }

    public async Task<IActionResult> Index(Guid? workspaceId, CancellationToken ct)
    {
        var list = workspaceId is null
            ? await _projects.GetAllAsync(ct)
            : await _projects.GetByWorkspaceIdAsync(workspaceId.Value, ct);
        var rows = list
            .Select(p => new ProjectListRow(p.Id, p.Name, p.WorkspaceId, p.Status))
            .ToList();
        ViewBag.FilterWorkspaceId = workspaceId;
        if (workspaceId is { } w)
            SetNav(w);
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? workspaceId, CancellationToken ct)
    {
        var workspaces = await _workspaces.GetAllAsync(ct);
        if (workspaces.Count == 0)
            return NotFound();

        var vm = new ProjectCreateFormVm
        {
            WorkspaceId = workspaceId ?? workspaces[0].Id,
            Status = ProjectStatus.Planning,
            Priority = ProjectPriority.Medium
        };
        ViewBag.Workspaces = workspaces;
        if (workspaceId is { } w)
            SetNav(w);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectCreateFormVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Workspaces = await _workspaces.GetAllAsync(ct);
            return View(model);
        }

        var result = await _projectService.CreateInWorkspaceAsync(
            model.WorkspaceId,
            model.Name,
            model.Description,
            model.Status,
            model.Priority,
            model.StartDate,
            model.DueDate,
            ct);

        if (result.IsSuccess)
            return RedirectToAction(nameof(Details), new { id = result.Value!.Id });

        if (result.Error!.Value.Kind == ErrorKind.NotFound)
            return NotFound();

        ModelState.AddModelError(string.Empty, result.Error.Value.Message);
        ViewBag.Workspaces = await _workspaces.GetAllAsync(ct);
        return View(model);
    }

    [HttpGet("/projects/{id:guid}/edit", Name = "project-edit-form")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var entity = await _projects.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        SetNav(entity.WorkspaceId, entity.Id);

        return View(new ProjectEditFormVm
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status,
            Priority = entity.Priority,
            StartDate = entity.StartDate,
            DueDate = entity.DueDate
        });
    }

    [HttpPost("/projects/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProjectEditFormVm model, CancellationToken ct)
    {
        model.Id = id;
        if (!ModelState.IsValid)
            return View(model);

        var result = await _projectService.UpdateAsync(
            id,
            model.Name,
            model.Description,
            model.Status,
            model.Priority,
            model.StartDate,
            model.DueDate,
            ct);

        if (result.IsSuccess)
            return RedirectToAction(nameof(Details), new { id });

        if (result.Error!.Value.Kind == ErrorKind.NotFound)
            return NotFound();

        ModelState.AddModelError(string.Empty, result.Error.Value.Message);
        return View(model);
    }

    [HttpGet("/projects/{id:guid}", Name = "project-details")]
    [HttpGet("/projects/{id:guid}/boards/{boardId:guid}", Name = "project-board-details")]
    public async Task<IActionResult> Details(Guid id, Guid? boardId, CancellationToken ct)
    {
        var entity = await _projects.GetByIdAsync(id, ct);
        if (entity is null)
            return NotFound();

        SetNav(entity.WorkspaceId, entity.Id);

        var boards = entity.Boards.OrderBy(b => b.Position).ThenBy(b => b.Name).ToList();
        Board? active = null;
        if (boardId is { } bid)
            active = boards.FirstOrDefault(b => b.Id == bid);
        active ??= boards.FirstOrDefault(b => b.IsDefault);
        active ??= boards.FirstOrDefault();

        var vm = new ProjectDetailsPageViewModel
        {
            Project = entity,
            ActiveBoard = active,
            BoardsOrdered = boards
        };
        ViewBag.Breadcrumbs = _breadcrumbs.ForProject(entity);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!await _projects.TryDeleteAsync(id, ct))
            return NotFound();
        return RedirectToAction(nameof(Index));
    }
}
