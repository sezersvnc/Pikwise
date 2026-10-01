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
        var specification = product.LaptopSpecification ??= new LaptopSpecification
        {
            Processor = input.Processor.Trim(), GPU = input.GPU.Trim(),
            Resolution = input.Resolution.Trim(), OperatingSystem = input.OperatingSystem.Trim()
        };
        specification.Processor = input.Processor.Trim();
        specification.GPU = input.GPU.Trim();
        specification.RamGb = input.RamGb;
        specification.StorageGb = input.StorageGb;
        specification.ScreenSize = input.ScreenSize;
        specification.Resolution = input.Resolution.Trim();
        specification.RefreshRate = input.RefreshRate;
        specification.Weight = input.Weight;
        specification.OperatingSystem = input.OperatingSystem.Trim();
    }

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
        product.LaptopSpecification is null
            ? null
            : new LaptopSpecificationDto(
                product.LaptopSpecification.Processor,
                product.LaptopSpecification.GPU,
                product.LaptopSpecification.RamGb,
                product.LaptopSpecification.StorageGb,
                product.LaptopSpecification.ScreenSize,
                product.LaptopSpecification.Resolution,
                product.LaptopSpecification.RefreshRate,
                product.LaptopSpecification.Weight,
                product.LaptopSpecification.OperatingSystem));
}
