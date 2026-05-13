using Microsoft.AspNetCore.Authorization;

namespace FlowCore.Services.Authorization;

public sealed record DemoUserRequirement : IAuthorizationRequirement;
