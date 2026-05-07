using FlowCore.Common;
using FlowCore.Models;

namespace FlowCore.Services.Domain;

public interface ICommentService
{
    Task<Result<Comment>> CreateAsync(Guid taskItemId, Guid authorUserId, string body, CancellationToken ct = default);
}
