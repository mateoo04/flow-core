namespace FlowCore.Models.ViewModels;

public enum SearchTab
{
    Projects,
    Tasks,
    Users
}

public sealed record SearchPanelRootVm(SearchTab DefaultTab);

public sealed record SearchResultsVm<TRow>(string Query, IReadOnlyList<TRow> Rows);

public sealed record SearchProjectRow(
    Guid Id,
    string Name,
    string? WorkspaceName);

public sealed record SearchTaskRow(
    Guid Id,
    string Title,
    string? ProjectName,
    string? StatusColorHex);

public sealed record SearchUserRow(
    Guid Id,
    string FullName,
    string Email,
    string Initials,
    string AvatarColor);
