using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pikwise.Api.RateLimiting;
using Pikwise.Application.Explanations.DTOs;
using Pikwise.Application.Explanations.Interfaces;
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
    IRequirementParsingService requirementParsingService,
    IExplanationService explanationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RecommendationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecommendationResponseDto>> Recommend(
        RecommendationRequestDto request, CancellationToken cancellationToken) =>
        Ok(await recommendationService.RecommendAsync(request, cancellationToken));

    // Returns criteria only; ranking stays in POST /api/recommendations, which the client calls next.
    // Calls the language model: signed-in users only, rate limited (ADR-026/028).
    [HttpPost("criteria")]
    [Authorize]
    [EnableRateLimiting(LanguageModelRateLimiting.PolicyName)]
    [ProducesResponseType<ParsedRequirementsResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ParsedRequirementsResponseDto>> ParseCriteria(
        NaturalLanguageRequestDto request, CancellationToken cancellationToken) =>
        Ok(await requirementParsingService.ParseAsync(request, cancellationToken));

    // Ranks with the same criteria as POST /api/recommendations, then explains that ranking.
    // A separate endpoint keeps the recommendation itself fast, free and deterministic.
    [HttpPost("explanation")]
    [Authorize]
    [EnableRateLimiting(LanguageModelRateLimiting.PolicyName)]
    [ProducesResponseType<ExplanationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ExplanationResponseDto>> Explain(
        RecommendationRequestDto request, CancellationToken cancellationToken) =>
        Ok(await explanationService.ExplainAsync(request, cancellationToken));
}
