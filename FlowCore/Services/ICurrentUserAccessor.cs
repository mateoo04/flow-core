using System.Security.Claims;
using FlowCore.Models;
using FlowCore.Repositories;
using Microsoft.AspNetCore.Http;

namespace FlowCore.Services;

public interface ICurrentUserAccessor
{
    Guid UserId { get; }
    Task<User?> GetAsync(CancellationToken ct = default);
}

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _ctx;
    private readonly IUserRepository _users;

    public CurrentUserAccessor(IHttpContextAccessor ctx, IUserRepository users)
    {
        _ctx = ctx;
        _users = users;
    }

    public Guid UserId
    {
        get
        {
            var raw = _ctx.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (raw is null) throw new InvalidOperationException("No authenticated user on this request.");
            return Guid.Parse(raw);
        }
    }

    public Task<User?> GetAsync(CancellationToken ct = default) => _users.GetByIdAsync(UserId, ct);
}
