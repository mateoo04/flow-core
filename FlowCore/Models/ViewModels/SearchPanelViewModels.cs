namespace FlowCore.Models.ViewModels;

public enum SearchTab
{
    Projects,
    Tasks,
    Users
}

public sealed record SearchPanelRootVm(SearchTab DefaultTab);

public sealed record GlobalSearchResultsVm(
    string Query,
    IReadOnlyList<GlobalSearchSectionVm> Sections);

public sealed record GlobalSearchSectionVm(
    string Key,
    string Title,
    IReadOnlyList<GlobalSearchRow> Rows,
    bool HasMore,
    int NextPage);

public sealed record GlobalSearchRow(
    string Title,
    string? Subtitle,
    string Href,
    string Kind,
    string? Initials = null,
    string? AvatarColor = null,
    string? StatusColorHex = null);
