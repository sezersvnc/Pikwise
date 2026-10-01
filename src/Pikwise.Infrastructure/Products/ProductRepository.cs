using Microsoft.EntityFrameworkCore;
using Pikwise.Application.Products;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.Infrastructure.Products;

public sealed class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.LaptopSpecification)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
}
