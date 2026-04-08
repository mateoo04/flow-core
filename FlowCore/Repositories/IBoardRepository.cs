using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IBoardRepository
{
    IReadOnlyList<Board> GetAll();

    IReadOnlyList<Board> GetByProjectId(Guid projectId);

    Board? GetById(Guid id);
}
