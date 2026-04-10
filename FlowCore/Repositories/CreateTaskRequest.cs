using FlowCore.Models;

namespace FlowCore.Repositories;

public sealed record CreateTaskRequest(
    Guid BoardColumnId,
    Guid TaskStatusDefinitionId,
    string Title,
    string? Description,
    TaskPriority Priority,
    int StoryPoints,
    Guid? ParentTaskItemId,
    DateTime? DueDate);
