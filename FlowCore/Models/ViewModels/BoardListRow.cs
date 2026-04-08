namespace FlowCore.Models.ViewModels;

public sealed record BoardListRow(Guid Id, string Name, Guid ProjectId, bool IsDefault, int ColumnCount);
