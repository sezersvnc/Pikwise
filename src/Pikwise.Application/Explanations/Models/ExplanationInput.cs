using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Recommendations.DTOs;

namespace Pikwise.Application.Explanations.Models;

// Everything the language model may use: the user's criteria, stored product facts and engine output.
// Null specification fields are unknown and must be described as unavailable, never guessed.
public sealed record ExplanationInput(
    RecommendationRequestDto Criteria,
    int CandidateCount,
    int RankedCount,
    IReadOnlyList<ExplanationProduct> Products,
    ValueAnalysisDto? ValueAnalysis);

public sealed record ExplanationProduct(
    int Rank,
    int ProductId,
    string Name,
    string Brand,
    decimal Price,
    LaptopSpecificationDto? Specification,
    decimal Score,
    IReadOnlyList<string> UnknownCriteria,
    IReadOnlyList<ExplanationScoreComponent> Components);

// Value is the measured input (GB, GB, tier, tier, kg, Hz); Contribution is in score points.
public sealed record ExplanationScoreComponent(string Criterion, decimal Value, decimal Contribution);
