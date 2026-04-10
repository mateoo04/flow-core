using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IProjectRepository
{
    IReadOnlyList<Project> GetAll();

    IReadOnlyList<Project> GetByWorkspaceId(Guid workspaceId);

    Project? GetById(Guid id);

    Project CreateInWorkspace(
        Guid workspaceId,
        string name,
        string description,
        ProjectStatus status,
        ProjectPriority priority);

    bool TryDelete(Guid id);
}
