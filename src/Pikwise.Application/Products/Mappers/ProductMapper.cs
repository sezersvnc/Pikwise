using Pikwise.Domain.Entities;
using Pikwise.Application.Products.DTOs;

namespace Pikwise.Application.Products.Mappers;

public static class ProductMapper
{
    public static void ApplyTo(this ProductWriteRequestDto request, Product product)
    {
        product.Name = request.Name.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.IsActive = request.IsActive;
        product.BrandId = request.BrandId;
        product.CategoryId = request.CategoryId;
        var input = request.Specification;
        // Reuse the existing specification on update to preserve its identity.
        var specification = product.LaptopSpecification ??= new LaptopSpecification();
        // Blank text is stored as null (unknown) rather than as an empty string.
        specification.Processor = NullIfBlank(input.Processor);
        specification.GPU = NullIfBlank(input.GPU);
        specification.RamGb = input.RamGb;
        specification.StorageGb = input.StorageGb;
        specification.ScreenSize = input.ScreenSize;
        specification.Resolution = NullIfBlank(input.Resolution);
        specification.RefreshRate = input.RefreshRate;
        specification.Weight = input.Weight;
        specification.OperatingSystem = NullIfBlank(input.OperatingSystem);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Read queries must load Brand, Category and LaptopSpecification before mapping.
    public static ProductResponseDto ToResponseDto(this Product product) => new(
        product.Id,
        product.Name,
        product.Price,
        product.Stock,
        product.IsActive,
        product.CreatedAt,
        product.UpdatedAt,
        new ProductBrandDto(product.Brand.Id, product.Brand.Name),
        new ProductCategoryDto(product.Category.Id, product.Category.Name),
        product.LaptopSpecification.ToSpecificationDto());

    // Comparison reads need Brand and LaptopSpecification, without Category or favorite loading.
    public static ProductComparisonItemDto ToComparisonDto(this Product product) => new(
        product.Id, product.Name, product.Price,
        new ProductBrandDto(product.Brand.Id, product.Brand.Name),
        product.LaptopSpecification.ToSpecificationDto());

    // Shared with recommendation results so every read exposes the same specification contract.
    internal static LaptopSpecificationDto? ToSpecificationDto(this LaptopSpecification? specification) =>
        specification is null ? null : new LaptopSpecificationDto(
            specification.Processor, specification.GPU, specification.RamGb, specification.StorageGb,
            specification.ScreenSize, specification.Resolution, specification.RefreshRate,
            specification.Weight, specification.OperatingSystem);
}
