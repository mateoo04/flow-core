using FlowCore.Common;
using FlowCore.Models;

namespace FlowCore.Services.Domain;

public interface IProjectService
{
    Task<Result<Project>> CreateInWorkspaceAsync(
        Guid workspaceId,
        string name,
        string description,
        ProjectStatus status,
        ProjectPriority priority,
        CancellationToken ct = default);
}
