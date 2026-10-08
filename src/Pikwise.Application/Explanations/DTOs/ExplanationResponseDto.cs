using Pikwise.Application.Recommendations.DTOs;

namespace Pikwise.Application.Explanations.DTOs;

// Recommendation is the deterministic ranking that was explained, returned together so both always match.
// Explanations follow the ranking order; ValueComment is null when there is no value analysis.
public sealed record ExplanationResponseDto(
    RecommendationResponseDto Recommendation,
    IReadOnlyList<ProductExplanationDto> Explanations,
    string? ValueComment);

public sealed record ProductExplanationDto(int ProductId, string Explanation);
