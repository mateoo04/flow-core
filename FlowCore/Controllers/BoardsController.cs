using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;

namespace FlowCore.Controllers;

public class BoardsController : BaseController
{
    private readonly IBoardRepository _boards;
    private readonly IProjectRepository _projects;
    private readonly IWorkspaceRepository _workspaces;
    private readonly IAuthorizationService _authz;

    public BoardsController(
        IBoardRepository boards,
        IProjectRepository projects,
        IWorkspaceRepository workspaces,
        IAuthorizationService authz)
    {
        _boards = boards;
        _projects = projects;
        _workspaces = workspaces;
        _authz = authz;
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
        {
            if (await EnsureWorkspaceMemberAsync(entity.Project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
            SetNav(entity.Project.WorkspaceId, entity.ProjectId);
        }

        return RedirectToAction(nameof(ProjectsController.Details), "Projects",
            new { id = entity.ProjectId, boardId = entity.Id });
    }

    [HttpGet("/projects/{projectId:guid}/boards/new", Name = "board-create-form")]
    public async Task<IActionResult> Create(Guid projectId, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project is null) return NotFound();
        if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        SetNav(project.WorkspaceId, project.Id);
        ViewBag.Project = project;
        return View(new BoardFormVm { ProjectId = projectId });
    }

    [HttpPost("/projects/{projectId:guid}/boards/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid projectId, BoardFormVm model, CancellationToken ct)
    {
        model.ProjectId = projectId;
        await ValidateUniqueAsync(model, excludeId: null, ct);

        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project is null) return NotFound();
        if (await EnsureWorkspaceMemberAsync(project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;

        if (!ModelState.IsValid)
        {
            ViewBag.Project = project;
            return View(model);
        }

        var board = await _boards.AddAsync(projectId, model.Name.Trim(), model.IsDefault, ct);
        return RedirectToAction(nameof(ProjectsController.Details), "Projects",
            new { id = projectId, boardId = board.Id });
    }

    [HttpGet("/boards/{id:guid}/edit", Name = "board-edit-form")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var entity = await _boards.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        if (entity.Project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(entity.Project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
            SetNav(entity.Project.WorkspaceId, entity.ProjectId);
        }

        ViewBag.Project = entity.Project;
        return View(new BoardFormVm
        {
            Id = entity.Id,
            ProjectId = entity.ProjectId,
            Name = entity.Name,
            IsDefault = entity.IsDefault
        });
    }

    [HttpPost("/boards/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BoardFormVm model, CancellationToken ct)
    {
        model.Id = id;
        var entity = await _boards.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        if (entity.Project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(entity.Project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
        }

        model.ProjectId = entity.ProjectId;
        await ValidateUniqueAsync(model, excludeId: id, ct);

        if (!ModelState.IsValid)
        {
            ViewBag.Project = entity.Project;
            return View(model);
        }

        var updated = await _boards.UpdateAsync(id, model.Name.Trim(), model.IsDefault, ct);
        if (updated is null) return NotFound();

        return RedirectToAction(nameof(ProjectsController.Details), "Projects",
            new { id = entity.ProjectId, boardId = id });
    }

    [HttpPost("/boards/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _boards.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        if (entity.Project is not null)
        {
            if (await EnsureWorkspaceMemberAsync(entity.Project.WorkspaceId, _workspaces, _authz, ct) is { } deny) return deny;
        }

        var projectId = entity.ProjectId;
        if (!await _boards.TryDeleteAsync(id, ct))
            return NotFound();

        return RedirectToAction(nameof(ProjectsController.Details), "Projects", new { id = projectId });
    }

    private async Task ValidateUniqueAsync(BoardFormVm model, Guid? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) return;
        if (await _boards.NameExistsInProjectAsync(model.ProjectId, model.Name, excludeId, ct))
            ModelState.AddModelError(nameof(BoardFormVm.Name), "A board with this name already exists in this project.");
    }
}
