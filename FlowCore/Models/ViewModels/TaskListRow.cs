using FlowCore.Models;

namespace FlowCore.Models.ViewModels;

public sealed record TaskListRow(
    Guid Id,
    string Title,
    TaskPriority Priority,
    Guid BoardColumnId,
    Guid? ParentTaskItemId);
