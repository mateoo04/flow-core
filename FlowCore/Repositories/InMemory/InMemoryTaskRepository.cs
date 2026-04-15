using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryTaskRepository(InMemoryDataStore store) => _store = store;

    public IReadOnlyList<TaskItem> GetAll()
    {
        lock (_store.Sync)
            return DemoDataLinqExamples.AllTasks(_store.Workspaces).ToList();
    }

    public IReadOnlyList<TaskItem> GetByBoardId(Guid boardId)
    {
        lock (_store.Sync)
        {
            var board = _store.FindBoard(boardId);
            return board?.Tasks.ToList() ?? (IReadOnlyList<TaskItem>)Array.Empty<TaskItem>();
        }
    }

    public TaskItem? GetById(Guid id)
    {
        lock (_store.Sync)
            return _store.FindTask(id);
    }

    public TaskItem? Create(CreateTaskRequest request)
    {
        lock (_store.Sync)
        {
            var status = _store.FindTaskStatusDefinition(request.TaskStatusDefinitionId);
            if (status is null)
                return null;

            Board? board;
            TaskItem? parent = null;

            if (request.ParentTaskItemId is { } parentId)
            {
                parent = _store.FindTask(parentId);
                if (parent?.Board is null)
                    return null;
                board = parent.Board;
            }
            else
            {
                board = _store.FindBoard(request.BoardId);
                if (board is null)
                    return null;
            }

            if (string.IsNullOrWhiteSpace(request.Title))
                return null;

            var now = DateTime.UtcNow;
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                BoardId = board.Id,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim() ?? "",
                TaskStatusDefinitionId = status.Id,
                Priority = request.Priority,
                StoryPoints = Math.Max(0, request.StoryPoints),
                ParentTaskItemId = parent?.Id,
                CreatedAt = now,
                UpdatedAt = now,
                DueDate = request.DueDate,
                Board = board,
                TaskStatusDefinition = status,
                ParentTaskItem = parent
            };

            board.Tasks.Add(task);
            status.TaskItems.Add(task);
            parent?.Subtasks.Add(task);
            return task;
        }
    }

    public bool TryDelete(Guid id)
    {
        lock (_store.Sync)
        {
            var task = _store.FindTask(id);
            if (task is null)
                return false;
            TaskGraphRemoval.RemoveTaskRecursive(_store, task);
            return true;
        }
    }
}
