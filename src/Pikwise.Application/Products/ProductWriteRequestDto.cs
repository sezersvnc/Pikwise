using System.ComponentModel.DataAnnotations;

namespace Pikwise.Application.Products;

public abstract class ProductWriteRequestDto
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true), DecimalScale(2)]
    public decimal Price { get; init; }
    [Range(0, int.MaxValue)]
    public int Stock { get; init; }
    public bool IsActive { get; init; }
    [Range(1, int.MaxValue)]
    public int BrandId { get; init; }
    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }
    [Required]
    public LaptopSpecificationRequestDto Specification { get; init; } = null!;
}

public sealed class LaptopSpecificationRequestDto
{
    [Required, StringLength(200)]
    public string Processor { get; init; } = string.Empty;
    [Required, StringLength(200)]
    public string GPU { get; init; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int RamGb { get; init; }
    [Range(1, int.MaxValue)]
    public int StorageGb { get; init; }
    [Range(typeof(decimal), "0.01", "999.99", ParseLimitsInInvariantCulture = true), DecimalScale(2)]
    public decimal ScreenSize { get; init; }
    [Required, StringLength(50)]
    public string Resolution { get; init; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int RefreshRate { get; init; }
    [Range(typeof(decimal), "0.001", "999.999", ParseLimitsInInvariantCulture = true), DecimalScale(3)]
    public decimal Weight { get; init; }
    [Required, StringLength(100)]
    public string OperatingSystem { get; init; } = string.Empty;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DecimalScaleAttribute(int scale) : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is decimal number && decimal.Round(number, scale) == number;

    public override string FormatErrorMessage(string name) =>
        $"{name} must have at most {scale} decimal places.";
}
