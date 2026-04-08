using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IBoardColumnRepository
{
    IReadOnlyList<BoardColumn> GetAll();

    IReadOnlyList<BoardColumn> GetByBoardId(Guid boardId);

    BoardColumn? GetById(Guid id);
}
