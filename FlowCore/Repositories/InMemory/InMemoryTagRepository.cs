using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryTagRepository : ITagRepository
{
    private readonly IReadOnlyList<Tag> _all;
    private readonly Dictionary<Guid, Tag> _byId;

    public InMemoryTagRepository(DemoDataGraph graph)
    {
        _all = graph.Tags.ToList();
        _byId = _all.ToDictionary(t => t.Id);
    }

    public IReadOnlyList<Tag> GetAll() => _all;

    public Tag? GetById(Guid id) =>
        _byId.TryGetValue(id, out var t) ? t : null;
}
