using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Models;

[PrimaryKey(nameof(TaskItemId), nameof(UserId))]
public class TaskAssignment
{
    [ForeignKey(nameof(TaskItem))]
    public Guid TaskItemId { get; set; }

    [ForeignKey("User")]
    public Guid UserId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual TaskItem? TaskItem { get; set; }
    public virtual User? User { get; set; }
}
