namespace FlowCore.Models.ViewModels;

public sealed class MoveTaskRequest
{
    public Guid StatusId { get; set; }
    public int Position { get; set; }
}
