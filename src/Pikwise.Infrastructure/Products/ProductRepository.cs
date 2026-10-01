using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Products.AsNoTracking().Include(p => p.Brand).Include(p => p.Category)
            .Include(p => p.LaptopSpecification).OrderBy(p => p.Id).ToListAsync(cancellationToken);

    public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        context.Products.Include(p => p.Brand).Include(p => p.Category).Include(p => p.LaptopSpecification)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Brand?> GetBrandAsync(int id, CancellationToken cancellationToken = default) =>
        context.Brands.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Category?> GetCategoryAsync(int id, CancellationToken cancellationToken = default) =>
        context.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Product product) => context.Products.Add(product);
    public void Remove(Product product) => context.Products.Remove(product);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConflictException(exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            throw new PersistenceConflictException(exception);
        }
    }
}
