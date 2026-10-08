using Pikwise.Application.Explanations.DTOs;
using Pikwise.Application.Recommendations.DTOs;

namespace Pikwise.Application.Explanations.Interfaces;

public interface IExplanationService
{
    Task<ExplanationResponseDto> ExplainAsync(RecommendationRequestDto request, CancellationToken cancellationToken = default);
}
