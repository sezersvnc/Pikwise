using Microsoft.AspNetCore.Authorization;
using Pikwise.Application.Users.Interfaces;

namespace Pikwise.Api.Authentication;

public sealed class LocalAdminRequirement : IAuthorizationRequirement;

public sealed class LocalAdminHandler(IUserProfileService profiles, IHttpContextAccessor accessor)
    : AuthorizationHandler<LocalAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        var profile = await profiles.GetCurrentAsync(accessor.HttpContext?.RequestAborted ?? default);
        // Supabase's role claim describes its database role; Pikwise privileges come from SQL Server.
        if (profile?.Role == "Admin") context.Succeed(requirement);
    }
}
