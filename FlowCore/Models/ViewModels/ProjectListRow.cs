using FlowCore.Models;

namespace FlowCore.Models.ViewModels;

public sealed record ProjectListRow(
    Guid Id,
    string Name,
    Guid WorkspaceId,
    ProjectStatus Status);
