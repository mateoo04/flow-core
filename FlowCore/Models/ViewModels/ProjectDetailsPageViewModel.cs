using FlowCore.Models;

namespace FlowCore.Models.ViewModels;

public sealed class ProjectDetailsPageViewModel
{
    public required Project Project { get; init; }
    public Board? ActiveBoard { get; init; }
    public IReadOnlyList<Board> BoardsOrdered { get; init; } = Array.Empty<Board>();
}
