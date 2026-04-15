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
    DateTime? DueDate);
