using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Products.Exceptions;
using Pikwise.Application.Products.Validators;

namespace Pikwise.UnitTests;

public class ProductRequestValidatorTests
{
    [Theory]
    [InlineData("", -1, -1, 0)]
    [InlineData("   ", 1, 1, 1)]
    [InlineData("Laptop", 1.001, 1, 1)]
    public void Invalid_product_fields_are_rejected(string name, double price, int stock, int brand)
    {
        var request = new CreateProductRequestDto
        {
            Name = name, Price = (decimal)price, Stock = stock, BrandId = brand, CategoryId = 1,
            Specification = ValidSpecification()
        };
        Assert.Throws<ProductValidationException>(() => ProductRequestValidator.Validate(request));
    }

    [Fact]
    public void Missing_specification_object_is_rejected()
    {
        var missing = new CreateProductRequestDto { Name = "Laptop", BrandId = 1, CategoryId = 1 };
        Assert.Throws<ProductValidationException>(() => ProductRequestValidator.Validate(missing));
    }

    [Fact]
    public void Specification_with_unknown_fields_is_accepted()
    {
        // A specification object is required, but each value inside it may be unknown (null).
        var empty = new CreateProductRequestDto
        {
            Name = "Laptop", BrandId = 1, CategoryId = 1, Specification = new LaptopSpecificationRequestDto()
        };
        ProductRequestValidator.Validate(empty);
        var partial = new UpdateProductRequestDto
        {
            Name = "Laptop", BrandId = 1, CategoryId = 1,
            Specification = new LaptopSpecificationRequestDto { RamGb = 16, Weight = 1.5m }
        };
        ProductRequestValidator.Validate(partial);
    }

    [Fact]
    public void Supplied_specification_values_are_still_validated()
    {
        var invalid = new UpdateProductRequestDto
        {
            Name = "Laptop", BrandId = 1, CategoryId = 1,
            Specification = new LaptopSpecificationRequestDto { RamGb = 0, Weight = 1.0005m, Processor = new string('x', 201) }
        };
        var error = Assert.Throws<ProductValidationException>(() => ProductRequestValidator.Validate(invalid));
        Assert.Contains("Specification.RamGb", error.Errors.Keys);
        Assert.Contains("Specification.Weight", error.Errors.Keys);
        Assert.Contains("Specification.Processor", error.Errors.Keys);
    }

    private static LaptopSpecificationRequestDto ValidSpecification() => new()
    {
        Processor = "CPU", GPU = "GPU", RamGb = 16, StorageGb = 512, ScreenSize = 15.6m,
        Resolution = "1920x1080", RefreshRate = 60, Weight = 1.875m, OperatingSystem = "OS"
    };
}
