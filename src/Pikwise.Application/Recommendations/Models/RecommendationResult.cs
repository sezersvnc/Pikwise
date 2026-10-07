using Pikwise.Domain.Entities;

namespace Pikwise.Application.Recommendations.Models;

// Ranked holds every product that passed all rules, best first; Top is what the API returns.
public sealed record RecommendationResult(
    IReadOnlyList<ScoredProduct> Ranked,
    int CandidateCount,
    int RemovedByEligibility,
    int RemovedByConstraints,
    int RemovedByKnownShare)
{
    public const int TopCount = 3;

    public IReadOnlyList<ScoredProduct> Top => Ranked.Take(TopCount).ToList();
}

// Score is 0..100, rounded half-up to two decimals. Importance values are sums of levels.
public sealed record ScoredProduct(
    Product Product,
    decimal Score,
    int KnownImportance,
    int TotalImportance,
    IReadOnlyList<RecommendationCriterion> UnknownCriteria,
    IReadOnlyList<ScoreComponent> Components)
{
    public decimal KnownWeightShare => RecommendationRounding.Component((decimal)KnownImportance / TotalImportance);
}

// Value is the measured input (GB, Hz, kg or the CPU/GPU tier). Contribution is in score points.
// These display values are rounded to four decimals; the score itself uses full precision.
public sealed record ScoreComponent(
    RecommendationCriterion Criterion,
    decimal Value,
    decimal NormalizedValue,
    decimal Weight,
    decimal Contribution);

public static class RecommendationRounding
{
    public static decimal Score(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal Component(decimal value) => decimal.Round(value, 4, MidpointRounding.AwayFromZero);
}
