namespace FlowCore.Models.ViewModels;

public sealed record TaskStatusDefinitionListRow(
    Guid Id,
    string Name,
    Guid ProjectId,
    string ColorHex,
    int Position,
    bool IsDoneState);
