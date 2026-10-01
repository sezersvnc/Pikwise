using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.IntegrationTests;

public class SqlServerRelationshipTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Migration_relationship_queries_and_constraints_work_on_sql_server()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set PIKWISE_TEST_CONNECTION to the dedicated SQL Server test database.");
        var settings = new SqlConnectionStringBuilder(connection);
        if (settings.InitialCatalog != "PikwiseSession3Tests")
            throw new InvalidOperationException("SQL tests require the dedicated PikwiseSession3Tests database.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        await using var transaction = await context.Database.BeginTransactionAsync();

        var unique = Guid.NewGuid().ToString("N");
        var brand = new Brand { Name = "TestBrand-" + unique };
        var category = new Category { Name = "Laptop-" + unique };
        var product = new Product
        {
            Name = "Relationship test laptop", Brand = brand, Category = category,
            Price = 42500.25m, Stock = 3, IsActive = true, CreatedAt = DateTimeOffset.UtcNow,
            LaptopSpecification = Specification()
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var productId = product.Id;
        var brandId = brand.Id;
        var categoryId = category.Id;
        context.ChangeTracker.Clear();

        var loaded = await context.Products.AsNoTracking()
            .Include(p => p.Brand).Include(p => p.Category).Include(p => p.LaptopSpecification)
            .SingleAsync(p => p.Id == productId);
        Assert.Equal(brandId, loaded.BrandId);
        Assert.Equal(categoryId, loaded.CategoryId);
        Assert.Equal(productId, loaded.LaptopSpecification!.ProductId);
        Assert.Equal(42500.25m, loaded.Price);

        var loadedBrand = await context.Brands.AsNoTracking().Include(b => b.Products)
            .ThenInclude(p => p.LaptopSpecification).SingleAsync(b => b.Id == brandId);
        Assert.Equal("Test CPU", Assert.Single(loadedBrand.Products).LaptopSpecification!.Processor);

        // Every failed write rolls back to EF's transaction savepoint.
        context.Brands.Add(new Brand { Name = brand.Name });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        context.Categories.Add(new Category { Name = category.Name });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        var duplicate = Specification();
        duplicate.ProductId = productId;
        context.LaptopSpecifications.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        var orphan = Specification();
        orphan.ProductId = int.MaxValue;
        context.LaptopSpecifications.Add(orphan);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        var invalidProduct = new Product
        {
            Name = "Orphan", BrandId = int.MaxValue, CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Products.Add(invalidProduct);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        invalidProduct.BrandId = brandId;
        invalidProduct.CategoryId = int.MaxValue;
        context.Products.Add(invalidProduct);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        context.Brands.Remove(await context.Brands.SingleAsync(b => b.Id == brandId));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        context.Categories.Remove(await context.Categories.SingleAsync(c => c.Id == categoryId));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        context.Products.Remove(await context.Products.SingleAsync(p => p.Id == productId));
        await context.SaveChangesAsync();
        Assert.False(await context.LaptopSpecifications.AnyAsync(s => s.ProductId == productId));
        Assert.True(await context.Brands.AnyAsync(b => b.Id == brandId));
        Assert.True(await context.Categories.AnyAsync(c => c.Id == categoryId));
        await transaction.RollbackAsync();
    }

    private static LaptopSpecification Specification() => new()
    {
        Processor = "Test CPU", GPU = "Test GPU", RamGb = 16, StorageGb = 512,
        ScreenSize = 15.6m, Resolution = "1920x1080", RefreshRate = 144,
        Weight = 1.875m, OperatingSystem = "Test OS"
    };
}
