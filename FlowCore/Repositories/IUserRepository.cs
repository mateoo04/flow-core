using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IUserRepository
{
    IReadOnlyList<User> GetAll();

    User? GetById(Guid id);
}
