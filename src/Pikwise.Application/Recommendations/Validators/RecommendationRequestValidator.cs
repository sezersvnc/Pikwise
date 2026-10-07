using System.ComponentModel.DataAnnotations;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Exceptions;

namespace Pikwise.Application.Recommendations.Validators;

public static class RecommendationRequestValidator
{
    public static void Validate(RecommendationRequestDto request)
    {
        // Validator does not descend into nested objects, so the importance block is checked separately.
        var errors = Collect(request, string.Empty);
        if (request.Importance is not null)
            errors.AddRange(Collect(request.Importance, nameof(RecommendationRequestDto.Importance) + "."));
        if (errors.Count == 0) return;
        throw new RecommendationValidationException(errors
            .GroupBy(error => error.Member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray()));
    }

    private static List<(string Member, string Message)> Collect(object instance, string prefix)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);
        return results.SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
            .Select(member => (prefix + member, result.ErrorMessage!))).ToList();
    }
}
