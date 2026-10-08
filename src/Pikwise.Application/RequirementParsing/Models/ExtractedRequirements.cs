using Pikwise.Application.Recommendations.DTOs;

namespace Pikwise.Application.RequirementParsing.Models;

// Deserialization target of the language model output; property names match RequirementExtractionContract.JsonSchema.
public sealed class ExtractedRequirements
{
    public decimal? BudgetMax { get; init; }
    public int? MinRamGb { get; init; }
    public int? MinStorageGb { get; init; }
    public decimal? MaxWeightKg { get; init; }
    public RecommendationImportanceDto? Importance { get; init; }
    public IReadOnlyList<string?>? Unsupported { get; init; }
}
