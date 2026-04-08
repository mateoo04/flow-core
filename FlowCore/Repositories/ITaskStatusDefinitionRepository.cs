using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ITaskStatusDefinitionRepository
{
    IReadOnlyList<TaskStatusDefinition> GetAll();

    IReadOnlyList<TaskStatusDefinition> GetByProjectId(Guid projectId);

    TaskStatusDefinition? GetById(Guid id);
}
