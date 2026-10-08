using Pikwise.Application.Explanations.Models;
using Pikwise.Application.Recommendations.DTOs;

namespace Pikwise.Application.Explanations.Mappers;

public static class ExplanationMapper
{
    // Copies only what the engine returned; normalized values and weights are left out to keep the input small.
    public static ExplanationInput ToExplanationInput(this RecommendationResponseDto recommendation, RecommendationRequestDto criteria) => new(
        criteria,
        recommendation.Summary.CandidateCount,
        recommendation.Summary.RankedCount,
        recommendation.Items.Select(item => new ExplanationProduct(
            item.Rank, item.ProductId, item.Name, item.Brand.Name, item.Price, item.Specification, item.Score,
            item.UnknownCriteria,
            item.Components.Select(component => new ExplanationScoreComponent(
                component.Criterion, component.Value, component.Contribution)).ToList())).ToList(),
        recommendation.ValueAnalysis);
}
