namespace FlowCore.Models.ViewModels;

public sealed record BoardColumnListRow(
    Guid Id,
    string Name,
    Guid BoardId,
    int Position,
    int TaskCount,
    bool IsDoneColumn);
