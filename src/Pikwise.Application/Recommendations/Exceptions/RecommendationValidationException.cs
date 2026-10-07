namespace Pikwise.Application.Recommendations.Exceptions;

public sealed class RecommendationValidationException(IDictionary<string, string[]> errors)
    : Exception("Recommendation request validation failed.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
