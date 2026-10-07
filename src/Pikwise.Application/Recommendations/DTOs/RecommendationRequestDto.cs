using System.ComponentModel.DataAnnotations;
using Pikwise.Application.Products.Validators;

namespace Pikwise.Application.Recommendations.DTOs;

// Every field is optional. Absent hard constraints are not applied; absent importance levels default to 3.
public sealed class RecommendationRequestDto
{
    [Range(typeof(decimal), "0.01", "10000000", ParseLimitsInInvariantCulture = true), DecimalScale(2)]
    public decimal? BudgetMax { get; init; }
    [Range(1, 256)]
    public int? MinRamGb { get; init; }
    [Range(1, 16384)]
    public int? MinStorageGb { get; init; }
    [Range(typeof(decimal), "0.1", "10", ParseLimitsInInvariantCulture = true), DecimalScale(2)]
    public decimal? MaxWeightKg { get; init; }
    public RecommendationImportanceDto? Importance { get; init; }
}

public sealed class RecommendationImportanceDto
{
    [Range(1, 5)]
    public int? Ram { get; init; }
    [Range(1, 5)]
    public int? Storage { get; init; }
    [Range(1, 5)]
    public int? Cpu { get; init; }
    [Range(1, 5)]
    public int? Gpu { get; init; }
    [Range(1, 5)]
    public int? Weight { get; init; }
    [Range(1, 5)]
    public int? RefreshRate { get; init; }
}
