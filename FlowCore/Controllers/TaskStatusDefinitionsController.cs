using Microsoft.AspNetCore.Mvc;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;

namespace FlowCore.Controllers;

public class TaskStatusDefinitionsController : BaseController
{
    private readonly ITaskStatusDefinitionRepository _statuses;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;

    public TaskStatusDefinitionsController(
        ITaskStatusDefinitionRepository statuses,
        IBreadcrumbTrailBuilder breadcrumbs)
    {
        _statuses = statuses;
        _breadcrumbs = breadcrumbs;
    }

    public IActionResult Index()
    {
        var rows = _statuses.GetAll()
            .Select(s => new TaskStatusDefinitionListRow(
                s.Id, s.Name, s.ProjectId, s.ColorHex, s.Position, s.IsDoneState))
            .OrderBy(r => r.ProjectId)
            .ThenBy(r => r.Position)
            .ToList();
        return View(rows);
    }

    public IActionResult Details(Guid id)
    {
        var entity = _statuses.GetById(id);
        if (entity?.Project is not null)
            SetNav(entity.Project.WorkspaceId, entity.ProjectId);
        return ViewDetails(entity, _breadcrumbs.ForTaskStatusDefinition);
    }
}
