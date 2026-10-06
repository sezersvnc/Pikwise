using Microsoft.EntityFrameworkCore;
using Pikwise.Application.ExternalProducts;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.Infrastructure.ExternalProducts;

public sealed class LaptopImportRepository(ApplicationDbContext context) : ILaptopImportRepository
{
    public Task<bool> ReferenceExistsAsync(string provider, string externalId, CancellationToken cancellationToken = default) =>
        context.ProductExternalReferences.AnyAsync(
            reference => reference.Provider == provider && reference.ExternalId == externalId, cancellationToken);

    public async Task<Brand> GetOrAddBrandAsync(string name, CancellationToken cancellationToken = default)
    {
        // Brand names are unique; the column collation decides case sensitivity, so the lookup is done in SQL.
        var brand = await context.Brands.FirstOrDefaultAsync(item => item.Name == name, cancellationToken);
        if (brand is not null) return brand;
        brand = new Brand { Name = name };
        context.Brands.Add(brand);
        return brand;
    }

    public async Task<Category> GetOrAddCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FirstOrDefaultAsync(item => item.Name == name, cancellationToken);
        if (category is not null) return category;
        category = new Category { Name = name };
        context.Categories.Add(category);
        return category;
    }

    public void Add(Product product) => context.Products.Add(product);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
