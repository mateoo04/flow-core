using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IWorkspaceRepository
{
    IReadOnlyList<Workspace> GetAll();

    Workspace? GetById(Guid id);
}
