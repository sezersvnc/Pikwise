namespace Pikwise.Application.Recommendations.Models;

// Value-for-money view of a ranking (RECOMMENDATION_ENGINE.md, Session 13). BestFit is rank 1.
// IsSmallGainUpgrade means a cheaper product is within the near-equal score gap, so paying for
// BestFit buys only a small suitability gain. Fit and value stay separate: the ranking is unchanged.
public sealed record ValueAnalysis(
    ScoredProduct BestFit,
    ScoredProduct BestValue,
    IReadOnlyList<CheaperAlternative> CheaperAlternatives)
{
    public bool IsSmallGainUpgrade => CheaperAlternatives.Count > 0;
}

// Rank is the 1-based position in the full ranking, so an alternative may come after the Top 3.
// ScoreGap and PriceDifference are relative to BestFit; PricePerPoint is the price of one score point.
public sealed record CheaperAlternative(
    ScoredProduct Product,
    int Rank,
    decimal ScoreGap,
    decimal PriceDifference,
    decimal PricePerPoint);
