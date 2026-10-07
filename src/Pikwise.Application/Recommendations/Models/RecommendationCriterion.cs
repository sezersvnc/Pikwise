namespace Pikwise.Application.Recommendations.Models;

// The six V1 scored criteria (RECOMMENDATION_ENGINE.md decision 10). The order is the output order.
public enum RecommendationCriterion
{
    Ram,
    Storage,
    Cpu,
    Gpu,
    Weight,
    RefreshRate
}
