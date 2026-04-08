using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly IReadOnlyList<User> _all;
    private readonly Dictionary<Guid, User> _byId;

    public InMemoryUserRepository(DemoDataGraph graph)
    {
        _all = graph.Users.ToList();
        _byId = _all.ToDictionary(u => u.Id);
    }

    public IReadOnlyList<User> GetAll() => _all;

    public User? GetById(Guid id) =>
        _byId.TryGetValue(id, out var u) ? u : null;
}
