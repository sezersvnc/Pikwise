using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pikwise.Application.Favorites.DTOs;
using Pikwise.Application.Favorites.Interfaces;

namespace Pikwise.Api.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public sealed class FavoritesController(IFavoriteService favorites) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<FavoriteResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<FavoriteResponseDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await favorites.GetAllAsync(cancellationToken));

    [HttpPost("{productId:int}")]
    [ProducesResponseType<FavoriteResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FavoriteResponseDto>> Add([Range(1, int.MaxValue)] int productId, CancellationToken cancellationToken)
    {
        var favorite = await favorites.AddAsync(productId, cancellationToken);
        return favorite is null ? NotFound() : StatusCode(StatusCodes.Status201Created, favorite);
    }

    [HttpDelete("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([Range(1, int.MaxValue)] int productId, CancellationToken cancellationToken) =>
        await favorites.DeleteAsync(productId, cancellationToken) ? NoContent() : NotFound();
}
