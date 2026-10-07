using Pikwise.Application.Products.DTOs;

namespace Pikwise.Application.Recommendations.DTOs;

// Items holds at most three products, best first. Summary explains how many products each rule removed.
public sealed record RecommendationResponseDto(
    IReadOnlyList<RecommendedProductDto> Items,
    RecommendationSummaryDto Summary);

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
