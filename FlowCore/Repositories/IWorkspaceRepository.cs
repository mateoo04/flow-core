using FlowCore.Models;

namespace FlowCore.Repositories;

public interface IWorkspaceRepository
{
    Task<IReadOnlyList<Workspace>> GetForUserAsync(Guid userId, CancellationToken ct = default);

    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Workspace> AddAsync(Workspace workspace, Guid ownerUserId, CancellationToken ct = default);

    Task<Workspace?> UpdateAsync(
        Guid id,
        string name,
        string description,
        CancellationToken ct = default);

    Task<bool> TryDeleteAsync(Guid id, CancellationToken ct = default);

    Task<bool> HasProjectsAsync(Guid workspaceId, CancellationToken ct = default);

    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default);

    Task<WorkspaceMember?> GetMembershipAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<WorkspaceMember>> GetMembersAsync(Guid workspaceId, CancellationToken ct = default);

    Task<WorkspaceMember?> AddMemberAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct = default);

    Task<bool> RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default);

    Task<bool> TransferOwnershipAsync(Guid workspaceId, Guid newOwnerUserId, CancellationToken ct = default);
}
