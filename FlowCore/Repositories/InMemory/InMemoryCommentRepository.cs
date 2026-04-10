using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryCommentRepository : ICommentRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryCommentRepository(InMemoryDataStore store) => _store = store;

    public IReadOnlyList<Comment> GetAll()
    {
        lock (_store.Sync)
            return DemoDataLinqExamples.AllTasks(_store.Workspaces)
                .SelectMany(t => t.Comments)
                .ToList();
    }

    public IReadOnlyList<Comment> GetByTaskItemId(Guid taskItemId)
    {
        lock (_store.Sync)
        {
            var task = _store.FindTask(taskItemId);
            return task?.Comments.OrderBy(c => c.CreatedAt).ToList()
                   ?? (IReadOnlyList<Comment>)Array.Empty<Comment>();
        }
    }

    public Comment? GetById(Guid id)
    {
        lock (_store.Sync)
            return DemoDataLinqExamples.AllTasks(_store.Workspaces)
                .SelectMany(t => t.Comments)
                .FirstOrDefault(c => c.Id == id);
    }

    public Comment? Create(Guid taskItemId, Guid authorUserId, string body)
    {
        lock (_store.Sync)
        {
            var task = _store.FindTask(taskItemId);
            if (task is null || string.IsNullOrWhiteSpace(body))
                return null;

            var c = new Comment
            {
                Id = Guid.NewGuid(),
                TaskItemId = taskItemId,
                AuthorUserId = authorUserId,
                Body = body.Trim(),
                CreatedAt = DateTime.UtcNow,
                EditedAt = null,
                TaskItem = task
            };
            task.Comments.Add(c);
            return c;
        }
    }

    public bool TryDelete(Guid id)
    {
        lock (_store.Sync)
        {
            var comment = DemoDataLinqExamples.AllTasks(_store.Workspaces)
                .SelectMany(t => t.Comments)
                .FirstOrDefault(c => c.Id == id);
            if (comment?.TaskItem is null)
                return false;
            comment.TaskItem.Comments.Remove(comment);
            return true;
        }
    }
}
