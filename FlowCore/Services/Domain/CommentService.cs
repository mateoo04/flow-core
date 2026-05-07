using FlowCore.Common;
using FlowCore.Data;
using FlowCore.Models;
using FlowCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Services.Domain;

public sealed class CommentService : ICommentService
{
    private readonly ICommentRepository _comments;
    private readonly FlowCoreDbContext _db;

    public CommentService(ICommentRepository comments, FlowCoreDbContext db)
    {
        _comments = comments;
        _db = db;
    }

    public async Task<Result<Comment>> CreateAsync(Guid taskItemId, Guid authorUserId, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Result.Validation<Comment>("Comment body is required.");

        if (!await _db.TaskItems.AnyAsync(t => t.Id == taskItemId, ct))
            return Result.NotFound<Comment>("Task not found.");

        if (!await _db.Users.AnyAsync(u => u.Id == authorUserId, ct))
            return Result.NotFound<Comment>("Author not found.");

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskItemId,
            AuthorUserId = authorUserId,
            Body = body.Trim(),
            CreatedAt = DateTime.UtcNow,
            EditedAt = null
        };

        return Result.Ok(await _comments.AddAsync(comment, ct));
    }
}
