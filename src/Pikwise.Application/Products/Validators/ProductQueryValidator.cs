using System.ComponentModel.DataAnnotations;
using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Products.Exceptions;

namespace Pikwise.Application.Products.Validators;

public static class ProductQueryValidator
{
    public static void Validate(ProductQueryRequestDto request)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(request, new ValidationContext(request), results, true)) return;
        var errors = results.SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (Member: member, Message: result.ErrorMessage!)))
            .GroupBy(error => error.Member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());
        throw new ProductValidationException(errors);
    }
}
