using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FlowCore.Models;

[PrimaryKey(nameof(TaskItemId), nameof(TagId))]
public class TaskTag
{
    [ForeignKey(nameof(TaskItem))]
    public Guid TaskItemId { get; set; }

    [ForeignKey(nameof(Tag))]
    public Guid TagId { get; set; }

    public DateTime LinkedAt { get; set; }

    public virtual TaskItem? TaskItem { get; set; }
    public virtual Tag? Tag { get; set; }
}
