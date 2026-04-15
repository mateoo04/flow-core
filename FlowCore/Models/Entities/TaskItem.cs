namespace FlowCore.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TaskStatusDefinitionId { get; set; }
    public TaskPriority Priority { get; set; }
    public int StoryPoints { get; set; }
    public Guid? ParentTaskItemId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }

    public Board? Board { get; set; }
    public TaskStatusDefinition? TaskStatusDefinition { get; set; }
    public TaskItem? ParentTaskItem { get; set; }
    public ICollection<TaskItem> Subtasks { get; set; } = new List<TaskItem>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}
