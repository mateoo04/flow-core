using FlowCore.Models;
using Microsoft.AspNetCore.Authorization;

namespace FlowCore.Services.Authorization;

public sealed record WorkspaceMembershipRequirement(WorkspaceRole MinimumRole) : IAuthorizationRequirement;
