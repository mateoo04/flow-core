using FlowCore.Models;

namespace FlowCore.Repositories;

public interface ITagRepository
{
    IReadOnlyList<Tag> GetAll();

    Tag? GetById(Guid id);
}
