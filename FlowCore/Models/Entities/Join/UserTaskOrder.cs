using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Models;

[PrimaryKey(nameof(UserId), nameof(TaskItemId))]
public class UserTaskOrder
{
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(TaskItem))]
    public Guid TaskItemId { get; set; }

    public int Position { get; set; }

    public virtual User? User { get; set; }
    public virtual TaskItem? TaskItem { get; set; }
}
