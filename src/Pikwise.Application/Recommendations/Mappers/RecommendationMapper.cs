using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Products.Mappers;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Models;
using Pikwise.Application.Recommendations.Scoring;

namespace Pikwise.Application.Recommendations.Mappers;

public static class RecommendationMapper
{
    public static UserRequirements ToRequirements(this RecommendationRequestDto request)
    {
        var importance = request.Importance;
        return new UserRequirements
        {
            BudgetMax = request.BudgetMax,
            MinRamGb = request.MinRamGb,
            MinStorageGb = request.MinStorageGb,
            MaxWeightKg = request.MaxWeightKg,
            Importance = new ImportanceLevels(
                importance?.Ram ?? ImportanceLevels.Default,
                importance?.Storage ?? ImportanceLevels.Default,
                importance?.Cpu ?? ImportanceLevels.Default,
                importance?.Gpu ?? ImportanceLevels.Default,
                importance?.Weight ?? ImportanceLevels.Default,
                importance?.RefreshRate ?? ImportanceLevels.Default)
        };
    }

    // Candidates must be loaded with Brand and LaptopSpecification before mapping.
    public static RecommendationResponseDto ToResponseDto(this RecommendationResult result, ValueAnalysis? value) => new(
        result.Top.Select((item, index) => item.ToDto(index + 1)).ToList(),
        new RecommendationSummaryDto(result.CandidateCount, result.RemovedByEligibility,
            result.RemovedByConstraints, result.RemovedByKnownShare, result.Ranked.Count),
        value?.ToDto());

    private static ValueAnalysisDto ToDto(this ValueAnalysis value) => new(
        value.BestFit.Product.Id,
        value.BestValue.Product.Id,
        value.IsSmallGainUpgrade,
        ValueAnalyzer.NearEqualScoreGap,
        value.CheaperAlternatives.Select(alternative => new CheaperAlternativeDto(
            alternative.Rank, alternative.Product.Product.Id, alternative.Product.Product.Name,
            alternative.Product.Product.Price, alternative.Product.Score, alternative.ScoreGap,
            alternative.PriceDifference, alternative.PricePerPoint)).ToList());

    private static RecommendedProductDto ToDto(this ScoredProduct item, int rank) => new(
        rank,
        item.Product.Id,
        item.Product.Name,
        item.Product.Price,
        new ProductBrandDto(item.Product.Brand.Id, item.Product.Brand.Name),
        item.Product.LaptopSpecification.ToSpecificationDto(),
        item.Score,
        item.KnownImportance,
        item.TotalImportance,
        item.KnownWeightShare,
        item.UnknownCriteria.Select(ToName).ToList(),
        item.Components.Select(component => new ScoreComponentDto(ToName(component.Criterion), component.Value,
            component.NormalizedValue, component.Weight, component.Contribution)).ToList());

    // Criterion names follow the request's importance property names in camelCase.
    public static string ToName(RecommendationCriterion criterion) => criterion switch
    {
        RecommendationCriterion.Ram => "ram",
        RecommendationCriterion.Storage => "storage",
        RecommendationCriterion.Cpu => "cpu",
        RecommendationCriterion.Gpu => "gpu",
        RecommendationCriterion.Weight => "weight",
        RecommendationCriterion.RefreshRate => "refreshRate",
        _ => throw new ArgumentOutOfRangeException(nameof(criterion), criterion, null)
    };
}
