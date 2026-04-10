using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ITaskRepository
{
    IReadOnlyList<TaskItem> GetAll();

    IReadOnlyList<TaskItem> GetByBoardColumnId(Guid boardColumnId);

    TaskItem? GetById(Guid id);

    TaskItem? Create(CreateTaskRequest request);

    bool TryDelete(Guid id);
}
