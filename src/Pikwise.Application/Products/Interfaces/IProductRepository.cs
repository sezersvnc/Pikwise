using Pikwise.Domain.Entities;

namespace Pikwise.Application.Products.Interfaces;

// Application defines the data-access contract; Infrastructure supplies the EF implementation.
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<Brand?> GetBrandAsync(int id, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryAsync(int id, CancellationToken cancellationToken = default);
    void Add(Product product);
    void Remove(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
