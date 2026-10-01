namespace Pikwise.Application.Products;

public sealed class ProductService(IProductRepository productRepository) : IProductService
{
    public async Task<ProductResponseDto?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        return product?.ToResponseDto();
    }
}
