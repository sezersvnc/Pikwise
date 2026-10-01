using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.IntegrationTests;

public class FavoriteRelationshipTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Favorites_enforce_unique_pairs_load_relationships_and_cascade_only_join_rows()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set PIKWISE_TEST_CONNECTION to the dedicated test database.");
        if (new SqlConnectionStringBuilder(connection).InitialCatalog != "PikwiseSession3Tests")
            throw new InvalidOperationException("SQL tests require PikwiseSession3Tests.");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var suffix = Guid.NewGuid().ToString("N");
        var time = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var firstUser = new UserProfile { AuthProviderUserId = "auth-" + suffix, Email = "first@example.test", CreatedAt = time };
        // External identity subjects are case-sensitive; these must remain distinct.
        var secondUser = new UserProfile { AuthProviderUserId = "AUTH-" + suffix, Email = "second@example.test", CreatedAt = time };
        var brand = new Brand { Name = "Favorite brand " + suffix };
        var category = new Category { Name = "Favorite category " + suffix };
        var firstProduct = Product("First laptop", brand, category, time);
        var secondProduct = Product("Second laptop", brand, category, time);
        context.AddRange(firstUser, secondUser, firstProduct, secondProduct);
        await context.SaveChangesAsync();
        var firstUserId = firstUser.Id;
        var secondUserId = secondUser.Id;
        var firstProductId = firstProduct.Id;
        var secondProductId = secondProduct.Id;
        context.Favorites.AddRange(
            new Favorite { UserProfileId = firstUserId, ProductId = firstProductId, CreatedAt = time },
            new Favorite { UserProfileId = firstUserId, ProductId = secondProductId, CreatedAt = time },
            new Favorite { UserProfileId = secondUserId, ProductId = firstProductId, CreatedAt = time });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loadedUser = await context.UserProfiles.AsNoTracking()
            .Include(u => u.Favorites).ThenInclude(f => f.Product).ThenInclude(p => p.LaptopSpecification)
            .SingleAsync(u => u.Id == firstUserId);
        Assert.Equal(2, loadedUser.Favorites.Count);
        Assert.All(loadedUser.Favorites, f =>
        {
            Assert.Equal(time, f.CreatedAt);
            Assert.Equal("CPU", f.Product.LaptopSpecification!.Processor);
        });
        var loadedProduct = await context.Products.AsNoTracking()
            .Include(p => p.Favorites).ThenInclude(f => f.UserProfile)
            .SingleAsync(p => p.Id == firstProductId);
        Assert.Equal(2, loadedProduct.Favorites.Count);
        Assert.Contains(loadedProduct.Favorites, f => f.UserProfile.Email == "first@example.test");

        context.Favorites.Add(new Favorite { UserProfileId = firstUserId, ProductId = firstProductId, CreatedAt = time });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        context.UserProfiles.Add(new UserProfile { AuthProviderUserId = firstUser.AuthProviderUserId, Email = "duplicate@example.test", CreatedAt = time });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        context.Favorites.Add(new Favorite { UserProfileId = int.MaxValue, ProductId = firstProductId, CreatedAt = time });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        context.Favorites.Add(new Favorite { UserProfileId = firstUserId, ProductId = int.MaxValue, CreatedAt = time });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        // Delete without loading favorites, so SQL Server's cascade is exercised.
        context.UserProfiles.Remove(await context.UserProfiles.SingleAsync(u => u.Id == firstUserId));
        await context.SaveChangesAsync();
        Assert.False(await context.Favorites.AnyAsync(f => f.UserProfileId == firstUserId));
        Assert.True(await context.Favorites.AnyAsync(f => f.UserProfileId == secondUserId && f.ProductId == firstProductId));
        Assert.True(await context.Products.AnyAsync(p => p.Id == firstProductId));
        context.Products.Remove(await context.Products.SingleAsync(p => p.Id == firstProductId));
        await context.SaveChangesAsync();
        Assert.False(await context.Favorites.AnyAsync(f => f.ProductId == firstProductId));
        Assert.True(await context.UserProfiles.AnyAsync(u => u.Id == secondUserId));
        Assert.True(await context.Products.AnyAsync(p => p.Id == secondProductId));
        await transaction.RollbackAsync();
    }

    private static Product Product(string name, Brand brand, Category category, DateTimeOffset time) => new()
    {
        Name = name, Brand = brand, Category = category, Price = 30000m, Stock = 1,
        IsActive = true, CreatedAt = time,
        LaptopSpecification = new LaptopSpecification
        {
            Processor = "CPU", GPU = "GPU", RamGb = 16, StorageGb = 512,
            ScreenSize = 15.6m, Resolution = "1920x1080", RefreshRate = 60,
            Weight = 1.8m, OperatingSystem = "OS"
        }
    };
}
