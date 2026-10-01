namespace Pikwise.Application.Products;

public interface IProductService
{
    Task<ProductResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductResponseDto> CreateAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default);
    Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
