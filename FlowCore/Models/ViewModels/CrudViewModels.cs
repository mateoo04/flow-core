using System.ComponentModel.DataAnnotations;
using FlowCore.Models;

namespace FlowCore.Models.ViewModels;

public sealed class ProjectCreateFormVm
{
    [Required]
    public Guid WorkspaceId { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    public ProjectPriority Priority { get; set; } = ProjectPriority.Medium;
}

public sealed class TaskCreateFormVm
{
    [Required]
    public Guid ProjectId { get; set; }

    public Guid BoardId { get; set; }

    [Required]
    public Guid BoardColumnId { get; set; }

    [Required]
    public Guid TaskStatusDefinitionId { get; set; }

    [Required]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public int StoryPoints { get; set; }

    public Guid? ParentTaskItemId { get; set; }

    public DateTime? DueDate { get; set; }
}

public sealed class CommentFormVm
{
    [Required]
    public string Body { get; set; } = "";
}

public sealed class TaskStatusFormVm
{
    [Required]
    public string Name { get; set; } = "";

    public string ColorHex { get; set; } = "#94A3B8";

    public bool IsDefault { get; set; }

    public bool IsDoneState { get; set; }
}
