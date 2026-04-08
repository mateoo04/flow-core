namespace FlowCore.Models.ViewModels;

public sealed class DashboardViewModel
{
    public int WorkspaceCount { get; init; }
    public int ProjectCount { get; init; }
    public int TaskCount { get; init; }
    public int UserCount { get; init; }
    public int TagCount { get; init; }
    public int HotTaskCount { get; init; }
    public IReadOnlyList<(string StatusName, int Count)> TasksByStatus { get; init; } = Array.Empty<(string, int)>();
    public IReadOnlyList<(Guid ProjectId, string WorkspaceName, string ProjectKey, string ProjectName, int TaskCount)>
        TopProjectsByTasks { get; init; } =
        Array.Empty<(Guid, string, string, string, int)>();
}
