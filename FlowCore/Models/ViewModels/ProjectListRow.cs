using FlowCore.Models;

namespace FlowCore.Models.ViewModels;

public sealed record ProjectListRow(
    Guid Id,
    string Key,
    string Name,
    Guid WorkspaceId,
    ProjectStatus Status);
