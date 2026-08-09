using System.ComponentModel.DataAnnotations;

namespace FlowCore.Models.Dtos;

public sealed class WorkspaceCreateDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [StringLength(2000)]
    public string Description { get; set; } = "";

    public WorkspaceVisibility Visibility { get; set; } = WorkspaceVisibility.Private;
}

public sealed class WorkspaceUpdateDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [StringLength(2000)]
    public string Description { get; set; } = "";

    public WorkspaceVisibility Visibility { get; set; } = WorkspaceVisibility.Private;
}

public sealed class ProjectCreateDto : IProjectInput
{
    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public ProjectPriority Priority { get; set; } = ProjectPriority.Medium;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public sealed class ProjectUpdateDto : IProjectInput
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public ProjectPriority Priority { get; set; } = ProjectPriority.Medium;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public sealed class BoardCreateDto
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required, StringLength(60, MinimumLength = 1)]
    public string Name { get; set; } = "";

    public int Position { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class BoardUpdateDto
{
    [Required, StringLength(60, MinimumLength = 1)]
    public string Name { get; set; } = "";

    public int Position { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class StatusCreateDto : IStatusInput
{
    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = "";

    public string ColorHex { get; set; } = "#94A3B8";

    public int Position { get; set; }
    public bool IsDoneState { get; set; }
}

public sealed class StatusUpdateDto : IStatusInput
{
    public string Name { get; set; } = "";

    public string ColorHex { get; set; } = "#94A3B8";

    public int Position { get; set; }
    public bool IsDoneState { get; set; }
}

public sealed class TagCreateDto : ITagInput
{
    public string Name { get; set; } = "";

    public string ColorHex { get; set; } = "#94A3B8";
}

public sealed class TagUpdateDto : ITagInput
{
    public string Name { get; set; } = "";

    public string ColorHex { get; set; } = "#94A3B8";
}

public sealed class TaskCreateDto : ITaskInput
{
    public Guid BoardId { get; set; }

    public Guid TaskStatusDefinitionId { get; set; }

    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int StoryPoints { get; set; }
    public Guid? ParentTaskItemId { get; set; }
    public DateTime? DueDate { get; set; }
    public List<Guid> AssigneeIds { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
}

public sealed class TaskUpdateDto : ITaskInput
{
    public Guid TaskStatusDefinitionId { get; set; }

    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int StoryPoints { get; set; }
    public DateTime? DueDate { get; set; }
    public List<Guid> AssigneeIds { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
}

public sealed class CommentCreateDto
{
    public Guid TaskItemId { get; set; }

    public Guid AuthorUserId { get; set; }

    public string Body { get; set; } = "";
}

public sealed class CommentUpdateDto
{
    public string Body { get; set; } = "";
}
