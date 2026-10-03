namespace Pikwise.Application.Products.DTOs;

// Reuse the specification contract so comparison exposes the same stored facts as product detail.
public sealed record ProductComparisonItemDto(
    int Id, string Name, decimal Price, ProductBrandDto Brand, LaptopSpecificationDto? Specification);
