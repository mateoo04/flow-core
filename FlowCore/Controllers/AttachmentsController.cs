using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Models.ViewModels;
using FlowCore.Repositories;
using FlowCore.Services;
using FlowCore.Services.Attachments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Controllers;

public class AttachmentsController : BaseController
{
    private readonly FlowCoreDbContext _db;
    private readonly IAttachmentStorage _storage;
    private readonly ImageUploadValidator _validator;
    private readonly IWorkspaceRepository _workspaces;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserAccessor _currentUser;

    public AttachmentsController(
        FlowCoreDbContext db,
        IAttachmentStorage storage,
        ImageUploadValidator validator,
        IWorkspaceRepository workspaces,
        IAuthorizationService authz,
        ICurrentUserAccessor currentUser)
    {
        _db = db;
        _storage = storage;
        _validator = validator;
        _workspaces = workspaces;
        _authz = authz;
        _currentUser = currentUser;
    }

    [HttpPost("/tasks/{taskId:guid}/attachments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(Guid taskId, IFormFile file, CancellationToken ct)
    {
        var workspaceId = await WorkspaceIdForTaskAsync(taskId, ct);
        if (workspaceId is null) return NotFound();

        var denied = await EnsureWorkspaceMemberAsync(workspaceId.Value, _workspaces, _authz, ct);
        if (denied is not null) return denied;

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File is empty." });

        var validation = _validator.Validate(file.FileName, file.ContentType, file.Length);
        if (!validation.IsValid)
            return BadRequest(new { message = validation.Error });

        var key = await _storage.SaveAsync(taskId, file, ct);

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskId,
            FileName = file.FileName,
            StoragePath = key,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedByUserId = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync(ct);

        return Ok(new { id = attachment.Id });
    }

    [HttpGet("/tasks/{taskId:guid}/attachments")]
    public async Task<IActionResult> List(Guid taskId, CancellationToken ct)
    {
        var workspaceId = await WorkspaceIdForTaskAsync(taskId, ct);
        if (workspaceId is null) return NotFound();

        var denied = await EnsureWorkspaceMemberAsync(workspaceId.Value, _workspaces, _authz, ct);
        if (denied is not null) return denied;

        var items = await _db.Attachments
            .AsNoTracking()
            .Where(a => a.TaskItemId == taskId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentListItem(a.Id, a.FileName, a.FileSize))
            .ToListAsync(ct);

        return PartialView("_AttachmentList", items);
    }

    [HttpGet("/attachments/{id:guid}/content")]
    public async Task<IActionResult> Content(Guid id, CancellationToken ct)
    {
        var attachment = await _db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (attachment is null) return NotFound();

        var workspaceId = await WorkspaceIdForTaskAsync(attachment.TaskItemId, ct);
        if (workspaceId is null) return NotFound();

        var denied = await EnsureWorkspaceMemberAsync(workspaceId.Value, _workspaces, _authz, ct);
        if (denied is not null) return denied;

        try
        {
            var stream = await _storage.OpenReadAsync(attachment.StoragePath, ct);
            return File(stream, attachment.ContentType);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("/attachments/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var attachment = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (attachment is null) return NotFound();

        var workspaceId = await WorkspaceIdForTaskAsync(attachment.TaskItemId, ct);
        if (workspaceId is null) return NotFound();

        var denied = await EnsureWorkspaceMemberAsync(workspaceId.Value, _workspaces, _authz, ct);
        if (denied is not null) return denied;

        await _storage.DeleteAsync(attachment.StoragePath, ct);
        _db.Attachments.Remove(attachment);
        await _db.SaveChangesAsync(ct);

        return Ok(new { success = true });
    }

    private async Task<Guid?> WorkspaceIdForTaskAsync(Guid taskId, CancellationToken ct)
    {
        var workspaceId = await _db.TaskItems
            .Where(t => t.Id == taskId)
            .Select(t => (Guid?)t.Board!.Project!.WorkspaceId)
            .FirstOrDefaultAsync(ct);
        return workspaceId;
    }
}
