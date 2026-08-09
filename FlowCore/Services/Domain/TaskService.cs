using FlowCore.Common;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Services.Domain;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _tasks;
    private readonly FlowCoreDbContext _db;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository tasks, FlowCoreDbContext db, ILogger<TaskService> logger)
    {
        _tasks = tasks;
        _db = db;
        _logger = logger;
    }

    public async Task<Result<TaskItem>> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Validation<TaskItem>("Title is required.");

        if (!await _db.TaskStatusDefinitions.AnyAsync(s => s.Id == request.TaskStatusDefinitionId, ct))
            return Result.NotFound<TaskItem>("Task status not found.");

        Guid boardId;
        Guid? parentId = null;

        if (request.ParentTaskItemId is { } pId)
        {
            var parentBoardId = await _db.TaskItems
                .Where(t => t.Id == pId)
                .Select(t => (Guid?)t.BoardId)
                .FirstOrDefaultAsync(ct);
            if (parentBoardId is null)
                return Result.NotFound<TaskItem>("Parent task not found.");
            boardId = parentBoardId.Value;
            parentId = pId;
        }
        else
        {
            if (!await _db.Boards.AnyAsync(b => b.Id == request.BoardId, ct))
                return Result.NotFound<TaskItem>("Board not found.");
            boardId = request.BoardId;
        }

        var validAssigneeIds = await ResolveValidAssigneeIdsAsync(request.AssigneeIds, ct);

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? "",
            TaskStatusDefinitionId = request.TaskStatusDefinitionId,
            Priority = request.Priority,
            StoryPoints = Math.Max(0, request.StoryPoints),
            ParentTaskItemId = parentId,
            CreatedAt = now,
            UpdatedAt = now,
            DueDate = NormalizeUtc(request.DueDate)
        };

        foreach (var userId in validAssigneeIds)
        {
            task.TaskAssignments.Add(new TaskAssignment
            {
                TaskItemId = task.Id,
                UserId = userId,
                AssignedAt = now
            });
        }

        var created = await _tasks.AddAsync(task, ct);
        _logger.LogInformation("Task created. {TaskId} {BoardId} {StatusId} {AssigneeCount}", created.Id, created.BoardId, created.TaskStatusDefinitionId, created.TaskAssignments.Count);
        return Result.Ok(created);
    }

    public async Task<Result<TaskItem>> UpdateAsync(UpdateTaskRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Validation<TaskItem>("Title is required.");

        var task = await _db.TaskItems
            .Include(t => t.TaskAssignments)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);
        if (task is null)
            return Result.NotFound<TaskItem>("Task not found.");

        if (!await _db.TaskStatusDefinitions.AnyAsync(s => s.Id == request.TaskStatusDefinitionId, ct))
            return Result.NotFound<TaskItem>("Task status not found.");

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim() ?? "";
        task.TaskStatusDefinitionId = request.TaskStatusDefinitionId;
        task.Priority = request.Priority;
        task.StoryPoints = Math.Max(0, request.StoryPoints);
        task.DueDate = NormalizeUtc(request.DueDate);
        task.UpdatedAt = DateTime.UtcNow;

        var validAssigneeIds = (await ResolveValidAssigneeIdsAsync(request.AssigneeIds, ct)).ToHashSet();

        var toRemove = task.TaskAssignments.Where(a => !validAssigneeIds.Contains(a.UserId)).ToList();
        foreach (var a in toRemove)
            _db.Remove(a);

        var existing = task.TaskAssignments.Select(a => a.UserId).ToHashSet();
        foreach (var userId in validAssigneeIds)
        {
            if (existing.Contains(userId)) continue;
            task.TaskAssignments.Add(new TaskAssignment
            {
                TaskItemId = task.Id,
                UserId = userId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Task updated. {TaskId} {StatusId} {AssigneeCount}", task.Id, task.TaskStatusDefinitionId, task.TaskAssignments.Count);
        return Result.Ok(task);
    }

    private static DateTime? NormalizeUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } v => v,
        var v => DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
    };

    private async Task<List<Guid>> ResolveValidAssigneeIdsAsync(IReadOnlyCollection<Guid> requested, CancellationToken ct)
    {
        if (requested.Count == 0) return new List<Guid>();
        var distinct = requested.Distinct().ToList();
        return await _db.Users
            .Where(u => u.IsActive && distinct.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(ct);
    }

    public async Task<Result<bool>> MoveAsync(
        Guid taskId,
        Guid destinationStatusId,
        int position,
        CancellationToken ct = default)
    {
        if (position < 0)
            return Result.Validation<bool>("Position must be non-negative.");

        var outcome = await _tasks.MoveAsync(taskId, destinationStatusId, position, ct);
        if (outcome == MoveResult.Moved)
            _logger.LogInformation("Task moved. {TaskId} {DestinationStatusId} {Position}", taskId, destinationStatusId, position);
        return outcome switch
        {
            MoveResult.Moved => Result.Ok(true),
            MoveResult.TaskNotFound => Result.NotFound<bool>("Task not found."),
            MoveResult.StatusNotFound => Result.NotFound<bool>("Status not found."),
            MoveResult.StatusInDifferentWorkspace =>
                Result.Conflict<bool>("Status does not belong to this task's workspace."),
            _ => Result.Validation<bool>("Unknown move result.")
        };
    }

    public async Task<Result<bool>> MoveOnHomeAsync(
        Guid currentUserId,
        Guid taskId,
        string destinationStatusName,
        int position,
        CancellationToken ct = default)
    {
        if (position < 0)
            return Result.Validation<bool>("Position must be non-negative.");
        if (string.IsNullOrWhiteSpace(destinationStatusName))
            return Result.Validation<bool>("Destination status name is required.");

        var outcome = await _tasks.MoveOnHomeAsync(currentUserId, taskId, destinationStatusName, position, ct);
        return outcome switch
        {
            MoveResult.Moved => Result.Ok(true),
            MoveResult.TaskNotFound => Result.NotFound<bool>("Task not found."),
            MoveResult.StatusNotFound =>
                Result.Conflict<bool>($"Workspace has no status named '{destinationStatusName}'."),
            _ => Result.Validation<bool>("Unknown move result.")
        };
    }
}
