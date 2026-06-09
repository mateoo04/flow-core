using System.ComponentModel.DataAnnotations;

namespace FlowCore.Models.Dtos;

// Request DTOs accepted from API clients. Validation lives in data-annotation
// attributes; [ApiController] auto-returns 400 when ModelState is invalid.
// Rules mirror the existing *FormVm classes in ViewModels/CrudViewModels.cs.

internal static class DtoValidation
{
    public const string HexColor = @"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$";
    public const string HexColorError = "Color must be a hex like #f00 or #ff0000.";
}

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

public sealed class ProjectCreateDto
{
    [Required]
    public Guid WorkspaceId { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [StringLength(2000)]
    public string Description { get; set; } = "";

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public ProjectPriority Priority { get; set; } = ProjectPriority.Medium;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public sealed class ProjectUpdateDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [StringLength(2000)]
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

public sealed class StatusCreateDto
{
    [Required]
    public Guid WorkspaceId { get; set; }

    [Required, StringLength(60, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Required, RegularExpression(DtoValidation.HexColor, ErrorMessage = DtoValidation.HexColorError)]
    public string ColorHex { get; set; } = "#94A3B8";

    public int Position { get; set; }
    public bool IsDoneState { get; set; }
}

public sealed class StatusUpdateDto
{
    [Required, StringLength(60, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Required, RegularExpression(DtoValidation.HexColor, ErrorMessage = DtoValidation.HexColorError)]
    public string ColorHex { get; set; } = "#94A3B8";

    public int Position { get; set; }
    public bool IsDoneState { get; set; }
}

public sealed class TagCreateDto
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Required, RegularExpression(DtoValidation.HexColor, ErrorMessage = DtoValidation.HexColorError)]
    public string ColorHex { get; set; } = "#94A3B8";
}

public sealed class TagUpdateDto
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Required, RegularExpression(DtoValidation.HexColor, ErrorMessage = DtoValidation.HexColorError)]
    public string ColorHex { get; set; } = "#94A3B8";
}

public sealed class TaskCreateDto
{
    [Required]
    public Guid BoardId { get; set; }

    [Required]
    public Guid TaskStatusDefinitionId { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int StoryPoints { get; set; }
    public Guid? ParentTaskItemId { get; set; }
    public DateTime? DueDate { get; set; }
    public List<Guid> AssigneeIds { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
}

public sealed class TaskUpdateDto
{
    [Required]
    public Guid TaskStatusDefinitionId { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int StoryPoints { get; set; }
    public DateTime? DueDate { get; set; }
    public List<Guid> AssigneeIds { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
}

public sealed class CommentCreateDto
{
    [Required]
    public Guid TaskItemId { get; set; }

    [Required]
    public Guid AuthorUserId { get; set; }

    [Required, StringLength(4000)]
    public string Body { get; set; } = "";
}

public sealed class CommentUpdateDto
{
    [Required, StringLength(4000)]
    public string Body { get; set; } = "";
}
