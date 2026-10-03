using System.ComponentModel.DataAnnotations;
using Pikwise.Application.Products.Validators;

namespace Pikwise.Application.Products.DTOs;

public sealed class ProductQueryRequestDto : IValidatableObject
{
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true), DecimalScale(2)]
    public decimal? MinPrice { get; init; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true), DecimalScale(2)]
    public decimal? MaxPrice { get; init; }
    [Range(1, int.MaxValue)]
    public int? BrandId { get; init; }
    [Range(1, int.MaxValue)]
    public int? MinRam { get; init; }
    [Range(1, int.MaxValue)]
    public int? MinStorage { get; init; }
    [StringLength(200)]
    public string? Cpu { get; init; }
    [StringLength(200)]
    public string? Gpu { get; init; }
    [Required]
    public string SortBy { get; init; } = "id";
    [Required]
    public string SortDirection { get; init; } = "asc";
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Query keywords use ordinal comparison, independent of the server's culture.
        if (!new[] { "id", "price", "name", "ram", "createdAt" }.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult("SortBy must be id, price, name, ram or createdAt.", [nameof(SortBy)]);
        if (!new[] { "asc", "desc" }.Contains(SortDirection, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult("SortDirection must be asc or desc.", [nameof(SortDirection)]);
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            yield return new ValidationResult("MinPrice must not exceed MaxPrice.", [nameof(MinPrice), nameof(MaxPrice)]);
        // EF's Skip accepts an Int32 offset; reject overflow before building a SQL query.
        if (Page > 0 && PageSize > 0 && ((long)Page - 1) * PageSize > int.MaxValue)
            yield return new ValidationResult("The requested page offset is too large.", [nameof(Page)]);
    }
}
