namespace FlowCore.Models;

public interface IProjectInput
{
    string Name { get; }
    string Description { get; }
    DateTime? StartDate { get; }
    DateTime? DueDate { get; }
}

public interface ITaskInput
{
    Guid TaskStatusDefinitionId { get; }
    string Title { get; }
    string? Description { get; }
    int StoryPoints { get; }
    List<Guid> AssigneeIds { get; }
}

public interface ITagInput
{
    string Name { get; }
    string ColorHex { get; }
}

public interface IStatusInput
{
    string Name { get; }
    string ColorHex { get; }
}
