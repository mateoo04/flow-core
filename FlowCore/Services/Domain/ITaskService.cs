using FlowCore.Common;
using FlowCore.Models;
using FlowCore.Repositories;

namespace FlowCore.Services.Domain;

public interface ITaskService
{
    Task<Result<TaskItem>> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);

    Task<Result<bool>> MoveAsync(
        Guid taskId,
        Guid destinationStatusId,
        int position,
        CancellationToken ct = default);

    Task<Result<bool>> MoveOnHomeAsync(
        Guid currentUserId,
        Guid taskId,
        string destinationStatusName,
        int position,
        CancellationToken ct = default);
}
