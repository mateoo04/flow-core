using System.ComponentModel.DataAnnotations;

namespace FlowCore.Models;

public class Tag
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;

    public virtual ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}
