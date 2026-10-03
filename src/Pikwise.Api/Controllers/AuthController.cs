using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pikwise.Application.Users.DTOs;
using Pikwise.Application.Users.Interfaces;

namespace Pikwise.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class AuthController(IUserProfileService profiles) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserProfileResponseDto>> Me(CancellationToken cancellationToken)
    {
        var profile = await profiles.GetCurrentAsync(cancellationToken);
        return profile is null ? Forbid() : Ok(profile);
    }

    // A small endpoint to exercise local authorization independently of future features.
    [HttpGet("admin-check")]
    [Authorize(Policy = "LocalAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminCheck() => NoContent();
}
