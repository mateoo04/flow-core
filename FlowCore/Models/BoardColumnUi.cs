namespace FlowCore.Models;

public static class BoardColumnUi
{
    private const string DefaultAccentHex = "#94a3b8";

    /// <summary>Hex color for column header dot; falls back when <see cref="BoardColumn.ColorHex"/> is unset.</summary>
    public static string AccentHex(BoardColumn column) =>
        string.IsNullOrWhiteSpace(column.ColorHex) ? DefaultAccentHex : column.ColorHex.Trim();
}
