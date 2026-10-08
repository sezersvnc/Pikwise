using Microsoft.AspNetCore.Mvc;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Interfaces;
using Pikwise.Application.RequirementParsing.DTOs;
using Pikwise.Application.RequirementParsing.Interfaces;

namespace Pikwise.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
// POST carries the structured requirements in the body; the request has no side effects.
public sealed class RecommendationsController(
    IRecommendationService recommendationService,
    IRequirementParsingService requirementParsingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RecommendationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecommendationResponseDto>> Recommend(
        RecommendationRequestDto request, CancellationToken cancellationToken) =>
        Ok(await recommendationService.RecommendAsync(request, cancellationToken));

    // Returns criteria only; ranking stays in POST /api/recommendations, which the client calls next.
    [HttpPost("criteria")]
    [ProducesResponseType<ParsedRequirementsResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ParsedRequirementsResponseDto>> ParseCriteria(
        NaturalLanguageRequestDto request, CancellationToken cancellationToken) =>
        Ok(await requirementParsingService.ParseAsync(request, cancellationToken));
}
