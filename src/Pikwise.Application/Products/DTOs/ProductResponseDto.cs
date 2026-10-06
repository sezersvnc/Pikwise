namespace Pikwise.Application.Products.DTOs;

// Expose catalog fields explicitly, keeping favorites and user profiles out of responses.
public sealed record ProductResponseDto(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    ProductBrandDto Brand,
    ProductCategoryDto Category,
    LaptopSpecificationDto? Specification);

public sealed record ProductBrandDto(int Id, string Name);

public sealed record ProductCategoryDto(int Id, string Name);

// A null value means the field is unknown; nothing is guessed or defaulted.
public sealed record LaptopSpecificationDto(
    string? Processor,
    string? GPU,
    int? RamGb,
    int? StorageGb,
    decimal? ScreenSize,
    string? Resolution,
    int? RefreshRate,
    decimal? Weight,
    string? OperatingSystem);
