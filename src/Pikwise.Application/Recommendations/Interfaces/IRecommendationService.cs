using Pikwise.Application.Recommendations.DTOs;

namespace Pikwise.Application.Recommendations.Interfaces;

public interface IRecommendationService
{
    Task<RecommendationResponseDto> RecommendAsync(RecommendationRequestDto request, CancellationToken cancellationToken = default);
}
