using Pikwise.Domain.Entities;

namespace Pikwise.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
