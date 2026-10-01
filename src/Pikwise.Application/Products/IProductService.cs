namespace Pikwise.Application.Products;

public interface IProductService
{
    Task<ProductResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
