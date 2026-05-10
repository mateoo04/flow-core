using FlowCore.Models;

namespace FlowCore.Repositories;

public sealed record CreateTaskRequest(
    Guid BoardId,
    Guid TaskStatusDefinitionId,
    string Title,
    string? Description,
    TaskPriority Priority,
    int StoryPoints,
    Guid? ParentTaskItemId,
    DateTime? DueDate,
    IReadOnlyCollection<Guid> AssigneeIds);

public sealed record UpdateTaskRequest(
    Guid Id,
    Guid TaskStatusDefinitionId,
    string Title,
    string? Description,
    TaskPriority Priority,
    int StoryPoints,
    DateTime? DueDate,
    IReadOnlyCollection<Guid> AssigneeIds);
