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

    public IReadOnlyList<TaskItem> GetByBoardColumnId(Guid boardColumnId)
    {
        lock (_store.Sync)
        {
            var col = _store.FindBoardColumn(boardColumnId);
            return col?.Tasks.ToList() ?? (IReadOnlyList<TaskItem>)Array.Empty<TaskItem>();
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
            if (status?.Project is null)
                return null;

            BoardColumn? column;
            TaskItem? parent = null;

            if (request.ParentTaskItemId is { } parentId)
            {
                parent = _store.FindTask(parentId);
                if (parent?.BoardColumn?.Board?.Project is null)
                    return null;
                var project = parent.BoardColumn!.Board!.Project!;
                if (project.Id != status.ProjectId)
                    return null;
                column = parent.BoardColumn;
            }
            else
            {
                column = _store.FindBoardColumn(request.BoardColumnId);
                if (column?.Board?.Project is null)
                    return null;
                if (column.Board.Project.Id != status.ProjectId)
                    return null;
            }

            if (string.IsNullOrWhiteSpace(request.Title))
                return null;

            var now = DateTime.UtcNow;
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                BoardColumnId = column.Id,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim() ?? "",
                TaskStatusDefinitionId = status.Id,
                Priority = request.Priority,
                StoryPoints = Math.Max(0, request.StoryPoints),
                ParentTaskItemId = parent?.Id,
                CreatedAt = now,
                UpdatedAt = now,
                DueDate = request.DueDate,
                BoardColumn = column,
                TaskStatusDefinition = status,
                ParentTaskItem = parent
            };

            column.Tasks.Add(task);
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
