using System.ComponentModel.DataAnnotations;

namespace Pikwise.Application.Products.DTOs;

public sealed class ProductComparisonRequestDto : IValidatableObject
{
    [Required, MinLength(2), MaxLength(3)]
    public int[] Ids { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Ids is null) yield break;
        if (Ids.Any(id => id <= 0))
            yield return new ValidationResult("Product IDs must be positive.", [nameof(Ids)]);
        if (Ids.Distinct().Count() != Ids.Length)
            yield return new ValidationResult("Choose different products for comparison.", [nameof(Ids)]);
    }
}
