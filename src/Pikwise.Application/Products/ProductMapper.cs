using Pikwise.Domain.Entities;

namespace Pikwise.Application.Products;

public static class ProductMapper
{
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
