using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryUserRepository(InMemoryDataStore store) => _store = store;

    public IReadOnlyList<User> GetAll()
    {
        lock (_store.Sync)
            return _store.Users.ToList();
    }

    public User? GetById(Guid id)
    {
        lock (_store.Sync)
            return _store.Users.FirstOrDefault(u => u.Id == id);
    }
}
