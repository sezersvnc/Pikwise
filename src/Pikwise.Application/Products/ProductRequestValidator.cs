using System.ComponentModel.DataAnnotations;

namespace Pikwise.Application.Products;

public static class ProductRequestValidator
{
    public static void Validate(ProductWriteRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateObject(request, "", errors);
        if (request.Specification is not null)
            ValidateObject(request.Specification, "Specification.", errors);
        if (errors.Count > 0) throw new ProductValidationException(errors);
    }

    private static void ValidateObject(object value, string prefix, Dictionary<string, string[]> errors)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, true);
        foreach (var result in results)
        foreach (var member in result.MemberNames)
            errors[prefix + member] = [result.ErrorMessage ?? "Invalid value."];
    }
}
