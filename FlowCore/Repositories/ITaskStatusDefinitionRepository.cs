using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ITaskStatusDefinitionRepository
{
    IReadOnlyList<TaskStatusDefinition> GetAll();

    IReadOnlyList<TaskStatusDefinition> GetByProjectId(Guid projectId);

    TaskStatusDefinition? GetById(Guid id);

    TaskStatusDefinition? Add(
        Guid projectId,
        string name,
        string colorHex,
        bool isDefault,
        bool isDoneState);

    bool TryDelete(Guid id, Guid? reassignToStatusId);
}
