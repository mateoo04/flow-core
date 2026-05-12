namespace FlowCore.Models.ViewModels;

public sealed class IconVm
{
    public required string Name { get; init; }
    public int Size { get; init; } = 20;
    public string CssClass { get; init; } = "";
    public string StrokeWidth { get; init; } = "1.5";
    public bool Filled { get; init; }
}
