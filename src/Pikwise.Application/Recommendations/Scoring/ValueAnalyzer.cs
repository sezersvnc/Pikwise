using Pikwise.Application.Recommendations.Models;

namespace Pikwise.Application.Recommendations.Scoring;

// Deterministic value-for-money analysis (Session 13, ADR-024). Pure function of the engine result:
// it never changes the ranking and never reads prices from anywhere but the ranked products.
public static class ValueAnalyzer
{
    // A cheaper product whose rounded score is at most this many points below the best fit is near-equal.
    public const decimal NearEqualScoreGap = 3m;

    public static ValueAnalysis? Analyze(RecommendationResult result)
    {
        var ranked = result.Ranked;
        if (ranked.Count == 0) return null;
        var bestFit = ranked[0];
        var alternatives = new List<CheaperAlternative>();
        for (var index = 1; index < ranked.Count; index++)
        {
            var item = ranked[index];
            // Gaps use the displayed (rounded) scores so the answer matches the numbers the user sees.
            var scoreGap = bestFit.Score - item.Score;
            var priceDifference = bestFit.Product.Price - item.Product.Price;
            // The ranking puts a cheaper product with an equal score first, so a cheaper item here has
            // a positive gap; the explicit check keeps the division safe regardless.
            if (priceDifference <= 0 || scoreGap <= 0 || scoreGap > NearEqualScoreGap) continue;
            var pricePerPoint = decimal.Round(priceDifference / scoreGap, 2, MidpointRounding.AwayFromZero);
            alternatives.Add(new CheaperAlternative(item, index + 1, scoreGap, priceDifference, pricePerPoint));
        }

        // Best value: the cheapest of the best fit and its near-equal alternatives; then score, then Id.
        var bestValue = alternatives.Select(alternative => alternative.Product).Prepend(bestFit)
            .OrderBy(item => item.Product.Price)
            .ThenByDescending(item => item.Score)
            .ThenBy(item => item.Product.Id)
            .First();
        return new ValueAnalysis(bestFit, bestValue, alternatives);
    }
}
