namespace FlowCore.Models;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;

    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}
