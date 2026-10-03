using Pikwise.Application.Products.DTOs;

namespace Pikwise.Application.Products.Interfaces;

// Controllers call product use cases through DTOs without depending on persistence details.
public interface IProductService
{
    Task<ProductResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductComparisonResponseDto> CompareAsync(ProductComparisonRequestDto request, CancellationToken cancellationToken = default);
    Task<PagedProductResponseDto> GetAllAsync(ProductQueryRequestDto query, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> CreateAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default);
    Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
