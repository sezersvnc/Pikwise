using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pikwise.Application.Products;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.IntegrationTests;

public class ProductCrudTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Crud_persists_full_graph_validates_requests_and_preserves_timestamps()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")!;
        Assert.Equal("PikwiseSession3Tests", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var database = new ApplicationDbContext(options);
        await database.Database.MigrateAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var brand = new Brand { Name = "CRUD brand " + suffix };
        var otherBrand = new Brand { Name = "CRUD other brand " + suffix };
        var category = new Category { Name = "CRUD category " + suffix };
        var otherCategory = new Category { Name = "CRUD other category " + suffix };
        database.AddRange(brand, otherBrand, category, otherCategory);
        await database.SaveChangesAsync();
        try
        {
            await using var factory = new PikwiseApiFactory(connection);
            using var client = factory.CreateClient();
            var body = new
            {
                name = "  CRUD laptop  ", price = 35000.25m, stock = 4, isActive = true,
                brandId = brand.Id, categoryId = category.Id,
                specification = new { processor = "CPU", gpu = "GPU", ramGb = 16, storageGb = 512,
                    screenSize = 15.6m, resolution = "1920x1080", refreshRate = 144, weight = 1.875m, operatingSystem = "OS" }
            };
            using var createdResponse = await client.PostAsJsonAsync("/api/products", body);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var created = (await createdResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!;
            Assert.Equal("CRUD laptop", created.Name);
            Assert.Null(created.UpdatedAt);
            Assert.Equal($"/api/products/{created.Id}", createdResponse.Headers.Location!.AbsolutePath);
            using var get = await client.GetAsync(createdResponse.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var list = (await client.GetFromJsonAsync<List<ProductResponseDto>>("/api/products"))!;
            Assert.Contains(list, p => p.Id == created.Id);

            var update = new { name = "Updated", price = 36000.50m, stock = 0, isActive = false,
                brandId = otherBrand.Id, categoryId = otherCategory.Id, specification = body.specification };
            using var updatedResponse = await client.PutAsJsonAsync($"/api/products/{created.Id}", update);
            Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
            var updated = (await updatedResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!;
            Assert.Equal(created.CreatedAt, updated.CreatedAt);
            Assert.NotNull(updated.UpdatedAt);
            Assert.Equal(otherBrand.Id, updated.Brand.Id);
            Assert.Equal(otherCategory.Id, updated.Category.Id);
            Assert.Equal(36000.50m, updated.Price);
            Assert.False(updated.IsActive);
            Assert.Equal(1, await database.LaptopSpecifications.CountAsync(s => s.ProductId == created.Id));

            using var badReferences = await client.PostAsJsonAsync("/api/products", new
            {
                body.name, body.price, body.stock, body.isActive, brandId = int.MaxValue,
                categoryId = int.MaxValue, body.specification
            });
            Assert.Equal(HttpStatusCode.BadRequest, badReferences.StatusCode);
            Assert.Equal("application/problem+json", badReferences.Content.Headers.ContentType!.MediaType);
            using var error = JsonDocument.Parse(await badReferences.Content.ReadAsStringAsync());
            Assert.True(error.RootElement.GetProperty("errors").TryGetProperty("BrandId", out _));

            using var invalid = await client.PostAsJsonAsync("/api/products", new
            {
                name = " ", price = -1m, stock = -1, brandId = brand.Id, categoryId = category.Id,
                specification = (object?)null
            });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using var tooPrecise = await client.PutAsJsonAsync($"/api/products/{created.Id}", new
            {
                body.name, price = 1.001m, body.stock, body.isActive, body.brandId, body.categoryId, body.specification
            });
            Assert.Equal(HttpStatusCode.BadRequest, tooPrecise.StatusCode);
            using var missingUpdate = await client.PutAsJsonAsync("/api/products/2147483647", body);
            Assert.Equal(HttpStatusCode.NotFound, missingUpdate.StatusCode);

            using var deleted = await client.DeleteAsync($"/api/products/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            Assert.False(await database.LaptopSpecifications.AnyAsync(s => s.ProductId == created.Id));
            using var missingGet = await client.GetAsync($"/api/products/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, missingGet.StatusCode);
            using var missingDelete = await client.DeleteAsync($"/api/products/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
            Assert.False(await database.Products.AnyAsync(p => p.BrandId == brand.Id || p.BrandId == otherBrand.Id));
        }
        finally
        {
            // Clean up only records belonging to this test's unique lookup rows.
            var products = await database.Products.Where(p => p.BrandId == brand.Id || p.BrandId == otherBrand.Id).ToListAsync();
            database.Products.RemoveRange(products);
            await database.SaveChangesAsync();
            database.RemoveRange(brand, otherBrand, category, otherCategory);
            await database.SaveChangesAsync();
        }
    }
}
