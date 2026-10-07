using Microsoft.AspNetCore.Mvc;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Interfaces;

namespace Pikwise.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
// POST carries the structured requirements in the body; the request has no side effects.
public sealed class RecommendationsController(IRecommendationService recommendationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RecommendationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecommendationResponseDto>> Recommend(
        RecommendationRequestDto request, CancellationToken cancellationToken) =>
        Ok(await recommendationService.RecommendAsync(request, cancellationToken));
}
