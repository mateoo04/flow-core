using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ITaskRepository
{
    IReadOnlyList<TaskItem> GetAll();

    IReadOnlyList<TaskItem> GetByBoardId(Guid boardId);

    TaskItem? GetById(Guid id);

    TaskItem? Create(CreateTaskRequest request);

    bool TryDelete(Guid id);
}
