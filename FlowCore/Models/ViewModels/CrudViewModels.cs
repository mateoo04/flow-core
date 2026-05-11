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

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }
}

public sealed class ProjectEditFormVm
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    public ProjectPriority Priority { get; set; } = ProjectPriority.Medium;

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }
}

public sealed class TaskCreateFormVm
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public Guid BoardId { get; set; }

    [Required]
    public Guid TaskStatusDefinitionId { get; set; }

    [Required]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public int StoryPoints { get; set; }

    public Guid? ParentTaskItemId { get; set; }

    public DateTime? DueDate { get; set; }

    public List<Guid> AssigneeIds { get; set; } = new();
}

public sealed class TaskEditFormVm
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid TaskStatusDefinitionId { get; set; }

    [Required]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public int StoryPoints { get; set; }

    public DateTime? DueDate { get; set; }

    public List<Guid> AssigneeIds { get; set; } = new();

    public IReadOnlyList<AutocompleteItem> SelectedAssignees { get; set; } = Array.Empty<AutocompleteItem>();
}

public sealed class CommentFormVm
{
    [Required]
    public string Body { get; set; } = "";
}

public sealed class UserFormVm
{
    public Guid? Id { get; set; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

public sealed class WorkspaceFormVm
{
    public Guid? Id { get; set; }

    [Required, StringLength(80, MinimumLength = 1)]
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public WorkspaceVisibility Visibility { get; set; } = WorkspaceVisibility.Private;
}

public sealed class BoardFormVm
{
    public Guid? Id { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    [Required, StringLength(60, MinimumLength = 1)]
    public string Name { get; set; } = "";

    public bool IsDefault { get; set; }
}

public sealed class TagFormVm
{
    public Guid? Id { get; set; }

    [Required, StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Required, RegularExpression(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Color must be a hex like #f00 or #ff0000.")]
    public string ColorHex { get; set; } = "#94A3B8";
}

public sealed class TaskStatusFormVm
{
    [Required]
    public string Name { get; set; } = "";

    public string ColorHex { get; set; } = "#94A3B8";

    public bool IsDoneState { get; set; }
}

public sealed record SettingsIndexVm(
    Workspace ActiveWorkspace,
    IReadOnlyList<TaskStatusDefinition> Statuses,
    IReadOnlyList<Workspace> AllWorkspaces);
