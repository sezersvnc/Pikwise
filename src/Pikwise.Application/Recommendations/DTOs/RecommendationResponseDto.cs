using Pikwise.Application.Products.DTOs;

namespace Pikwise.Application.Recommendations.DTOs;

// Items holds at most three products, best first. Summary explains how many products each rule removed.
// ValueAnalysis is null when no product qualifies.
public sealed record RecommendationResponseDto(
    IReadOnlyList<RecommendedProductDto> Items,
    RecommendationSummaryDto Summary,
    ValueAnalysisDto? ValueAnalysis);

// Best fit is rank 1; best value is the cheapest of the best fit and its near-equal cheaper alternatives.
public sealed record ValueAnalysisDto(
    int BestFitProductId,
    int BestValueProductId,
    bool IsSmallGainUpgrade,
    decimal NearEqualScoreGap,
    IReadOnlyList<CheaperAlternativeDto> CheaperAlternatives);

// Rank is the position in the full ranking and can be greater than 3. Gaps are relative to the best fit.
public sealed record CheaperAlternativeDto(
    int Rank,
    int ProductId,
    string Name,
    decimal Price,
    decimal Score,
    decimal ScoreGap,
    decimal PriceDifference,
    decimal PricePerPoint);

public sealed record RecommendationSummaryDto(
    int CandidateCount,
    int RemovedByEligibility,
    int RemovedByConstraints,
    int RemovedByKnownShare,
    int RankedCount);

// Specification repeats the stored facts; a null field there is unknown, never guessed.
public sealed record RecommendedProductDto(
    int Rank,
    int ProductId,
    string Name,
    decimal Price,
    ProductBrandDto Brand,
    LaptopSpecificationDto? Specification,
    decimal Score,
    int KnownImportance,
    int TotalImportance,
    decimal KnownWeightShare,
    IReadOnlyList<string> UnknownCriteria,
    IReadOnlyList<ScoreComponentDto> Components);

// Value is the measured input (GB, GB, tier, tier, kg, Hz); Contribution is in score points.
public sealed record ScoreComponentDto(
    string Criterion,
    decimal Value,
    decimal NormalizedValue,
    decimal Weight,
    decimal Contribution);
