using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Pikwise.Application.Products.Interfaces;
using Pikwise.Application.Products.Exceptions;
using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Products.Models;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.Infrastructure.Products;

public sealed class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    // Read-only queries avoid tracking and load the relationships required by the response mapper.
    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.LaptopSpecification)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<ProductPageResult> GetAllAsync(ProductQueryRequestDto query, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> products = context.Products.AsNoTracking();
        if (query.MinPrice is { } minPrice) products = products.Where(p => p.Price >= minPrice);
        if (query.MaxPrice is { } maxPrice) products = products.Where(p => p.Price <= maxPrice);
        if (query.BrandId is { } brandId) products = products.Where(p => p.BrandId == brandId);
        if (query.MinRam is { } minRam)
            products = products.Where(p => p.LaptopSpecification != null && p.LaptopSpecification.RamGb >= minRam);
        if (query.MinStorage is { } minStorage)
            products = products.Where(p => p.LaptopSpecification != null && p.LaptopSpecification.StorageGb >= minStorage);
        if (!string.IsNullOrWhiteSpace(query.Cpu))
        {
            var cpu = query.Cpu.Trim();
            products = products.Where(p => p.LaptopSpecification != null && p.LaptopSpecification.Processor.Contains(cpu));
        }
        if (!string.IsNullOrWhiteSpace(query.Gpu))
        {
            var gpu = query.Gpu.Trim();
            products = products.Where(p => p.LaptopSpecification != null && p.LaptopSpecification.GPU.Contains(gpu));
        }

        // Count the filtered catalog, then fetch only the requested page. Both execute in SQL.
        var totalCount = await products.CountAsync(cancellationToken);
        var descending = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        var ordered = query.SortBy.ToLowerInvariant() switch
        {
            "price" => descending ? products.OrderByDescending(p => p.Price) : products.OrderBy(p => p.Price),
            "name" => descending ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name),
            "ram" => descending ? products.OrderByDescending(p => p.LaptopSpecification!.RamGb) : products.OrderBy(p => p.LaptopSpecification!.RamGb),
            "createdat" => descending ? products.OrderByDescending(p => p.CreatedAt) : products.OrderBy(p => p.CreatedAt),
            _ => descending ? products.OrderByDescending(p => p.Id) : products.OrderBy(p => p.Id)
        };
        // A unique tie-breaker keeps equal sort values in a predictable order across pages.
        var items = await ordered.ThenBy(p => p.Id)
            .Skip(checked((query.Page - 1) * query.PageSize)).Take(query.PageSize)
            .Include(p => p.Brand).Include(p => p.Category).Include(p => p.LaptopSpecification)
            .ToListAsync(cancellationToken);
        return new ProductPageResult(items, totalCount);
    }

    // Writes use tracked entities so SaveChanges detects changes to the product and specification.
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
        // Translate persistence failures into an Application exception; API chooses the HTTP status.
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConflictException(exception);
        }
        // SQL error 547 covers constraint conflicts, including a referenced row deleted after validation.
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            throw new PersistenceConflictException(exception);
        }
    }
}
