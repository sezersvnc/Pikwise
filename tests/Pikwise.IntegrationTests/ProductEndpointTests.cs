using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Pikwise.Application.Products.DTOs;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.IntegrationTests;

public class ProductEndpointTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task GetById_returns_dto_and_missing_product_returns_404()
    {
        var connection = TestDatabaseConnection();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection).Options;
        int productId;
        int brandId;
        int categoryId;

        await using (var arrangeContext = new ApplicationDbContext(options))
        {
            await arrangeContext.Database.MigrateAsync();
            var suffix = Guid.NewGuid().ToString("N");
            var product = new Product
            {
                Name = "API test laptop",
                Price = 51999.95m,
                Stock = 8,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2026, 10, 1, 15, 0, 0, TimeSpan.Zero),
                Brand = new Brand { Name = "API Brand " + suffix },
                Category = new Category { Name = "API Category " + suffix },
                LaptopSpecification = new LaptopSpecification
                {
                    Processor = "API CPU", GPU = "API GPU", RamGb = 32, StorageGb = 1024,
                    ScreenSize = 16m, Resolution = "2560x1600", RefreshRate = 165,
                    Weight = 1.9m, OperatingSystem = "API OS"
                }
            };
            arrangeContext.Products.Add(product);
            await arrangeContext.SaveChangesAsync();
            productId = product.Id;
            brandId = product.BrandId;
            categoryId = product.CategoryId;
        }

        try
        {
            await using var factory = new PikwiseApiFactory(connection);
            using var client = factory.CreateClient();

            using var foundResponse = await client.GetAsync($"/api/products/{productId}");
            var response = await foundResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
            using var missingResponse = await client.GetAsync("/api/products/2147483647");

            Assert.Equal(HttpStatusCode.OK, foundResponse.StatusCode);
            Assert.NotNull(response);
            Assert.Equal(productId, response.Id);
            Assert.Equal("API test laptop", response.Name);
            Assert.Equal("API CPU", response.Specification?.Processor);
            Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            var product = await cleanupContext.Products.FindAsync(productId);
            if (product is not null)
            {
                cleanupContext.Products.Remove(product);
                await cleanupContext.SaveChangesAsync();
            }

            var brand = await cleanupContext.Brands.FindAsync(brandId);
            var category = await cleanupContext.Categories.FindAsync(categoryId);
            if (brand is not null) cleanupContext.Brands.Remove(brand);
            if (category is not null) cleanupContext.Categories.Remove(category);
            await cleanupContext.SaveChangesAsync();
        }
    }

    private static string TestDatabaseConnection()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set PIKWISE_TEST_CONNECTION to the dedicated SQL Server test database.");
        var database = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog;
        return database == "PikwiseSession3Tests"
            ? connection
            : throw new InvalidOperationException("SQL tests require the dedicated PikwiseSession3Tests database.");
    }
}
