using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ICommentRepository
{
    IReadOnlyList<Comment> GetAll();

    IReadOnlyList<Comment> GetByTaskItemId(Guid taskItemId);

    Comment? GetById(Guid id);

    Comment? Create(Guid taskItemId, Guid authorUserId, string body);

    bool TryDelete(Guid id);
}
