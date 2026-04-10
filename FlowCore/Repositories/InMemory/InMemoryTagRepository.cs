using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryTagRepository : ITagRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryTagRepository(InMemoryDataStore store) => _store = store;

    public IReadOnlyList<Tag> GetAll()
    {
        lock (_store.Sync)
            return _store.Tags.ToList();
    }

    public Tag? GetById(Guid id)
    {
        lock (_store.Sync)
            return _store.Tags.FirstOrDefault(t => t.Id == id);
    }
}
