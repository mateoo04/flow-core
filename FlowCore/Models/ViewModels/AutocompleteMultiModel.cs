namespace FlowCore.Models.ViewModels;

public sealed class AutocompleteMultiModel
{
    public required string FieldName { get; init; }
    public required string SearchUrl { get; init; }
    public string Placeholder { get; init; } = "Search…";
    public string EmptyHint { get; init; } = "Start typing to search…";
    public IReadOnlyList<AutocompleteItem> Selected { get; init; } = Array.Empty<AutocompleteItem>();
}

public sealed record AutocompleteItem(
    Guid Id,
    string Label,
    string? Sublabel,
    string Initials,
    string BackgroundHex);

public sealed record AutocompleteChipVm(AutocompleteItem Item, string FieldName);

public sealed record AutocompleteResultListVm(
    IReadOnlyList<AutocompleteChipVm> Items,
    string EmptyMessage = "No matches.");
