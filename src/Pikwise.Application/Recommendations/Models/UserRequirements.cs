namespace Pikwise.Application.Recommendations.Models;

// Structured input of the engine. Every hard constraint is optional: null means not applied.
public sealed record UserRequirements
{
    public decimal? BudgetMax { get; init; }
    public int? MinRamGb { get; init; }
    public int? MinStorageGb { get; init; }
    public decimal? MaxWeightKg { get; init; }
    public ImportanceLevels Importance { get; init; } = new();
}

// Importance levels 1..5 per scored criterion; a criterion the user does not mention gets 3.
public sealed record ImportanceLevels(
    int Ram = ImportanceLevels.Default,
    int Storage = ImportanceLevels.Default,
    int Cpu = ImportanceLevels.Default,
    int Gpu = ImportanceLevels.Default,
    int Weight = ImportanceLevels.Default,
    int RefreshRate = ImportanceLevels.Default)
{
    public const int Default = 3;

    public int For(RecommendationCriterion criterion) => criterion switch
    {
        RecommendationCriterion.Ram => Ram,
        RecommendationCriterion.Storage => Storage,
        RecommendationCriterion.Cpu => Cpu,
        RecommendationCriterion.Gpu => Gpu,
        RecommendationCriterion.Weight => Weight,
        RecommendationCriterion.RefreshRate => RefreshRate,
        _ => throw new ArgumentOutOfRangeException(nameof(criterion), criterion, null)
    };
}
