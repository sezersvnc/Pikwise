using Pikwise.Application.Recommendations.DTOs;

namespace Pikwise.Application.RequirementParsing.DTOs;

// Criteria use the POST /api/recommendations request shape, so the client can review and send them unchanged.
// Unsupported lists wishes no criterion covers; they are reported, never guessed into a criterion.
public sealed record ParsedRequirementsResponseDto(
    RecommendationRequestDto Criteria,
    IReadOnlyList<string> Unsupported);
