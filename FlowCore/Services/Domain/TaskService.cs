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

    public TaskService(ITaskRepository tasks, FlowCoreDbContext db)
    {
        _tasks = tasks;
        _db = db;
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
            DueDate = request.DueDate
        };

        return Result.Ok(await _tasks.AddAsync(task, ct));
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
