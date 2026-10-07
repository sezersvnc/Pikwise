using Pikwise.Application.Recommendations.Models;
using Pikwise.Domain.Entities;

namespace Pikwise.Application.Recommendations.Scoring;

// Deterministic Recommendation Engine V1 (RECOMMENDATION_ENGINE.md decisions 1-18).
// Pure function of its inputs: no I/O, no clock, no randomness, no LLM.
public static class RecommendationEngine
{
    // Products whose known criteria carry less than this share of the total importance are not recommended.
    public const decimal MinimumKnownShare = 0.5m;

    private sealed record ReferenceRange(decimal Min, decimal Max, bool LowerIsBetter = false)
    {
        public decimal Normalize(decimal value)
        {
            var position = (Math.Clamp(value, Min, Max) - Min) / (Max - Min);
            return LowerIsBetter ? 1 - position : position;
        }
    }

    // Fixed reference ranges (decision 14). Values outside a range are clamped.
    private static readonly ReferenceRange RamRange = new(8, 32);
    private static readonly ReferenceRange StorageRange = new(256, 1024);
    private static readonly ReferenceRange WeightRange = new(1.0m, 2.5m, LowerIsBetter: true);
    private static readonly ReferenceRange RefreshRateRange = new(60, 165);
    private static readonly ReferenceRange TierRange = new(LaptopPerformanceTiers.MinTier, LaptopPerformanceTiers.MaxTier);

    public static RecommendationResult Recommend(IEnumerable<Product> products, UserRequirements requirements)
    {
        var candidates = products.ToList();
        var eligible = candidates.Where(IsEligible).ToList();
        var constrained = eligible.Where(product => SatisfiesConstraints(product, requirements)).ToList();
        var scored = constrained.Select(product => Score(product, requirements.Importance)).ToList();
        // Compare integer sums instead of a rounded share so the 50% boundary is exact.
        var qualified = scored.Where(item => item.KnownImportance >= MinimumKnownShare * item.TotalImportance).ToList();
        // Rounded score first (decision 17), then price ascending, then Id ascending (decision 8).
        var ranked = qualified
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Product.Price)
            .ThenBy(item => item.Product.Id)
            .ToList();
        return new RecommendationResult(ranked, candidates.Count,
            candidates.Count - eligible.Count, eligible.Count - constrained.Count, scored.Count - qualified.Count);
    }

    // Eligibility applies to recommendation only; catalog and comparison still show these products.
    private static bool IsEligible(Product product) => product.IsActive && product.Stock > 0;

    // Strict hard constraints. A field required by an active constraint must be known (decision 13).
    private static bool SatisfiesConstraints(Product product, UserRequirements requirements)
    {
        var specification = product.LaptopSpecification;
        if (requirements.BudgetMax is { } budget && product.Price > budget) return false;
        if (requirements.MinRamGb is { } minRam && !(specification?.RamGb >= minRam)) return false;
        if (requirements.MinStorageGb is { } minStorage && !(specification?.StorageGb >= minStorage)) return false;
        if (requirements.MaxWeightKg is { } maxWeight && !(specification?.Weight <= maxWeight)) return false;
        return true;
    }

    private static ScoredProduct Score(Product product, ImportanceLevels importance)
    {
        var known = new List<(RecommendationCriterion Criterion, decimal Value, decimal Normalized, int Level)>();
        var unknown = new List<RecommendationCriterion>();
        var totalImportance = 0;
        foreach (var criterion in Enum.GetValues<RecommendationCriterion>())
        {
            var level = importance.For(criterion);
            totalImportance += level;
            var measurement = Measure(product.LaptopSpecification, criterion);
            if (measurement is { } m) known.Add((criterion, m.Value, m.Range.Normalize(m.Value), level));
            else unknown.Add(criterion);
        }

        // Unknown criteria are left out and the known weights are rescaled to sum to 1 (decision 6).
        var knownImportance = known.Sum(item => item.Level);
        var weightedSum = known.Sum(item => item.Level * item.Normalized);
        var score = knownImportance == 0 ? 0 : RecommendationRounding.Score(100 * weightedSum / knownImportance);
        var components = known.Select(item =>
        {
            var weight = (decimal)item.Level / knownImportance;
            return new ScoreComponent(item.Criterion, item.Value, RecommendationRounding.Component(item.Normalized),
                RecommendationRounding.Component(weight), RecommendationRounding.Component(100 * weight * item.Normalized));
        }).ToList();
        return new ScoredProduct(product, score, knownImportance, totalImportance, unknown, components);
    }

    // Returns null when the value is unknown: a missing specification, a null field or an unlisted CPU/GPU.
    private static (decimal Value, ReferenceRange Range)? Measure(LaptopSpecification? specification, RecommendationCriterion criterion)
    {
        if (specification is null) return null;
        decimal? value = criterion switch
        {
            RecommendationCriterion.Ram => specification.RamGb,
            RecommendationCriterion.Storage => specification.StorageGb,
            RecommendationCriterion.Cpu => LaptopPerformanceTiers.GetCpuTier(specification.Processor),
            RecommendationCriterion.Gpu => LaptopPerformanceTiers.GetGpuTier(specification.GPU),
            RecommendationCriterion.Weight => specification.Weight,
            RecommendationCriterion.RefreshRate => specification.RefreshRate,
            _ => throw new ArgumentOutOfRangeException(nameof(criterion), criterion, null)
        };
        var range = criterion switch
        {
            RecommendationCriterion.Ram => RamRange,
            RecommendationCriterion.Storage => StorageRange,
            RecommendationCriterion.Weight => WeightRange,
            RecommendationCriterion.RefreshRate => RefreshRateRange,
            _ => TierRange
        };
        return value is { } known ? (known, range) : null;
    }
}
