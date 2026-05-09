using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowCore.Models;

public class TaskItem
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(Board))]
    public Guid BoardId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [ForeignKey(nameof(TaskStatusDefinition))]
    public Guid TaskStatusDefinitionId { get; set; }

    public TaskPriority Priority { get; set; }
    public int StoryPoints { get; set; }

    public int Position { get; set; }

    [ForeignKey(nameof(ParentTaskItem))]
    public Guid? ParentTaskItemId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }

    public virtual Board? Board { get; set; }
    public virtual TaskStatusDefinition? TaskStatusDefinition { get; set; }
    public virtual TaskItem? ParentTaskItem { get; set; }
    public virtual ICollection<TaskItem> Subtasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
    public virtual ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}
