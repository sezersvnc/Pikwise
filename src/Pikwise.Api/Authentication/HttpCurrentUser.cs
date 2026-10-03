using Pikwise.Application.Users.Interfaces;

namespace Pikwise.Api.Authentication;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    // Only the principal established by JWT validation supplies the identity.
    public string? Subject => accessor.HttpContext?.User.Identity?.IsAuthenticated == true
        ? accessor.HttpContext.User.FindFirst("sub")?.Value : null;
    public string? Email => accessor.HttpContext?.User.Identity?.IsAuthenticated == true
        ? accessor.HttpContext.User.FindFirst("email")?.Value : null;
}
