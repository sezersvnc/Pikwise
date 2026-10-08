using System.ComponentModel.DataAnnotations;
using Pikwise.Application.Recommendations.Exceptions;
using Pikwise.Application.RequirementParsing.DTOs;

namespace Pikwise.Application.RequirementParsing.Validators;

public static class NaturalLanguageRequestValidator
{
    public static void Validate(NaturalLanguageRequestDto request)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(request, new ValidationContext(request), results, true)) return;
        throw new RecommendationValidationException(results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (Member: member, Message: result.ErrorMessage!)))
            .GroupBy(error => error.Member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray()));
    }
}
