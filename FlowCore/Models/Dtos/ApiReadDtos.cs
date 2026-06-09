namespace FlowCore.Models.Dtos;

// Read DTOs returned to API clients. Entities are never serialized directly:
// navigation properties would cause cyclic/oversized JSON and leak internal fields.

public sealed record WorkspaceSummaryDto(Guid Id, string Name);

public sealed record StatusSummaryDto(Guid Id, string Name, string ColorHex);

public sealed record UserSummaryDto(Guid Id, string FullName, string? Email);

public sealed record TagDto(Guid Id, string Name, string ColorHex);

public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime? ArchivedAt,
    WorkspaceVisibility Visibility);

public sealed record ProjectDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string Description,
    DateTime StartDate,
    DateTime? DueDate,
    ProjectStatus Status,
    ProjectPriority Priority,
    WorkspaceSummaryDto? Workspace);

public sealed record BoardDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    int Position,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record StatusDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string ColorHex,
    int Position,
    bool IsDoneState,
    DateTime CreatedAt);

public sealed record TaskItemDto(
    Guid Id,
    Guid BoardId,
    string Title,
    string Description,
    Guid TaskStatusDefinitionId,
    StatusSummaryDto? Status,
    TaskPriority Priority,
    int StoryPoints,
    int Position,
    Guid? ParentTaskItemId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DueDate,
    IReadOnlyList<UserSummaryDto> Assignees,
    IReadOnlyList<TagDto> Tags);

public sealed record CommentDto(
    Guid Id,
    Guid TaskItemId,
    Guid AuthorUserId,
    UserSummaryDto? Author,
    string Body,
    DateTime CreatedAt,
    DateTime? EditedAt);
