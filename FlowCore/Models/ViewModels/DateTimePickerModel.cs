namespace FlowCore.Models.ViewModels;

public sealed class DateTimePickerModel
{
    public required string FieldName { get; init; }
    public DateTime? Value { get; init; }
    public string? Placeholder { get; init; }
    public bool Required { get; init; }
    public bool IncludeTime { get; init; } = true;
}
