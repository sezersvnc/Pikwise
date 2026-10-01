using Pikwise.Domain.Entities;

namespace Pikwise.Application.Products;

public sealed class ProductService(IProductRepository productRepository) : IProductService
{
    public async Task<ProductResponseDto?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        return product?.ToResponseDto();
    }

    public async Task<IReadOnlyList<ProductResponseDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await productRepository.GetAllAsync(cancellationToken)).Select(p => p.ToResponseDto()).ToList();

    public async Task<ProductResponseDto> CreateAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        ProductRequestValidator.Validate(request);
        var (brand, category) = await GetReferencesAsync(request, cancellationToken);
        var product = new Product { Name = request.Name.Trim(), CreatedAt = DateTimeOffset.UtcNow };
        request.ApplyTo(product);
        product.Brand = brand;
        product.Category = category;
        productRepository.Add(product);
        await productRepository.SaveChangesAsync(cancellationToken);
        return product.ToResponseDto();
    }

    public async Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        ProductRequestValidator.Validate(request);
        var product = await productRepository.GetForUpdateAsync(id, cancellationToken);
        if (product is null) return null;
        var (brand, category) = await GetReferencesAsync(request, cancellationToken);
        request.ApplyTo(product);
        product.Brand = brand;
        product.Category = category;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await productRepository.SaveChangesAsync(cancellationToken);
        return product.ToResponseDto();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetForUpdateAsync(id, cancellationToken);
        if (product is null) return false;
        productRepository.Remove(product);
        await productRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<(Brand, Category)> GetReferencesAsync(ProductWriteRequestDto request, CancellationToken cancellationToken)
    {
        var brand = await productRepository.GetBrandAsync(request.BrandId, cancellationToken);
        var category = await productRepository.GetCategoryAsync(request.CategoryId, cancellationToken);
        var errors = new Dictionary<string, string[]>();
        if (brand is null) errors[nameof(request.BrandId)] = ["Brand does not exist."];
        if (category is null) errors[nameof(request.CategoryId)] = ["Category does not exist."];
        if (errors.Count > 0) throw new ProductValidationException(errors);
        return (brand!, category!);
    }
}
