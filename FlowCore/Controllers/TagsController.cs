using Microsoft.AspNetCore.Mvc;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using FlowCore.Validation;
using FluentValidation;

namespace FlowCore.Controllers;

public class TagsController : BaseController
{
    private readonly ITagRepository _tags;
    private readonly IBreadcrumbTrailBuilder _breadcrumbs;
    private readonly IValidator<TagFormVm> _validator;

    public TagsController(ITagRepository tags, IBreadcrumbTrailBuilder breadcrumbs, IValidator<TagFormVm> validator)
    {
        _tags = tags;
        _breadcrumbs = breadcrumbs;
        _validator = validator;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var tags = await _tags.GetAllAsync(ct);
        var rows = tags
            .Select(t => new TagListRow(t.Id, t.Name, t.ColorHex))
            .ToList();
        return View(rows);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var entity = await _tags.GetByIdAsync(id, ct);
        return ViewDetails(entity, _breadcrumbs.ForTag);
    }

    [HttpGet("/tags/create", Name = "tag-create-form")]
    public IActionResult Create()
    {
        return View(new TagFormVm());
    }

    [HttpPost("/tags/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TagFormVm model, CancellationToken ct)
    {
        await ValidateUniqueNameAsync(model, excludeId: null, ct);
        await this.ValidateAndAddToModelStateAsync(_validator, model, ct);
        if (!ModelState.IsValid)
            return View(model);

        var tag = await _tags.AddAsync(new Tag
        {
            Id = Guid.NewGuid(),
            Name = model.Name.Trim(),
            ColorHex = model.ColorHex.Trim()
        }, ct);

        return RedirectToAction(nameof(Details), new { id = tag.Id });
    }

    [HttpGet("/tags/{id:guid}/edit", Name = "tag-edit-form")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var entity = await _tags.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        return View(new TagFormVm
        {
            Id = entity.Id,
            Name = entity.Name,
            ColorHex = entity.ColorHex
        });
    }

    [HttpPost("/tags/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TagFormVm model, CancellationToken ct)
    {
        model.Id = id;
        await ValidateUniqueNameAsync(model, excludeId: id, ct);
        await this.ValidateAndAddToModelStateAsync(_validator, model, ct);
        if (!ModelState.IsValid)
            return View(model);

        var updated = await _tags.UpdateAsync(id, model.Name.Trim(), model.ColorHex.Trim(), ct);
        if (updated is null) return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("/tags/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!await _tags.TryDeleteAsync(id, ct)) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueNameAsync(TagFormVm model, Guid? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) return;
        if (await _tags.NameExistsAsync(model.Name, excludeId, ct))
            ModelState.AddModelError(nameof(TagFormVm.Name), "A tag with this name already exists.");
    }
}
