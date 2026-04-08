using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IProjectRepository
{
    IReadOnlyList<Project> GetAll();

    IReadOnlyList<Project> GetByWorkspaceId(Guid workspaceId);

    Project? GetById(Guid id);
}
