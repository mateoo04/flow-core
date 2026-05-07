using FlowCore.Common;
using FlowCore.Models;
using FlowCore.Repositories;

namespace FlowCore.Services.Domain;

public interface ITaskService
{
    Task<Result<TaskItem>> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
}
