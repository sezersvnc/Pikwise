using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Interfaces;
using Pikwise.Application.Recommendations.Mappers;
using Pikwise.Application.Recommendations.Scoring;
using Pikwise.Application.Recommendations.Validators;

namespace Pikwise.Application.Recommendations.Services;

// Coordinates validation, candidate loading, deterministic ranking and response mapping.
public sealed class RecommendationService(IRecommendationRepository recommendationRepository) : IRecommendationService
{
    public async Task<RecommendationResponseDto> RecommendAsync(
        RecommendationRequestDto request, CancellationToken cancellationToken = default)
    {
        // Validate here too, so callers outside MVC receive the same request rules.
        RecommendationRequestValidator.Validate(request);
        var candidates = await recommendationRepository.GetCandidatesAsync(cancellationToken);
        return RecommendationEngine.Recommend(candidates, request.ToRequirements()).ToResponseDto();
    }
}
