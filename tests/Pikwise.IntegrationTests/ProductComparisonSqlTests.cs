using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pikwise.Application.Products.DTOs;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;
using Pikwise.Infrastructure.Products;

namespace Pikwise.IntegrationTests;

public class ProductComparisonSqlTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Compare_returns_two_or_three_products_handles_missing_ids_and_loads_one_sql_query()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")!;
        Assert.Equal("PikwiseSession3Tests", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var database = new ApplicationDbContext(options);
        await database.Database.MigrateAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var brand = new Brand { Name = "Comparison brand " + suffix };
        var category = new Category { Name = "Comparison category " + suffix };
        Product Laptop(string name, decimal price, int ram) => new()
        {
            Name = name, Price = price, Brand = brand, Category = category, CreatedAt = DateTimeOffset.UtcNow,
            LaptopSpecification = new LaptopSpecification
            {
                Processor = "CPU " + name, GPU = "GPU " + name, RamGb = ram, StorageGb = 512,
                ScreenSize = 15.6m, Resolution = "1920x1080", RefreshRate = 144, Weight = 1.875m, OperatingSystem = "Test OS"
            }
        };
        var first = Laptop("A", 30000.25m, 16);
        var second = Laptop("B", 50000.50m, 32);
        var missingSpec = Laptop("C", 40000, 16);
        missingSpec.LaptopSpecification = null;
        var unselected = Laptop("Not selected", 10000, 8);
        Product[] products = [first, second, missingSpec, unselected];
        database.AddRange(products);
        await database.SaveChangesAsync();
        try
        {
            await using var factory = new PikwiseApiFactory(connection);
            using var client = factory.CreateClient();
            using var twoResponse = await client.GetAsync($"/api/products/compare?ids={second.Id}&ids={first.Id}");
            Assert.Equal(HttpStatusCode.OK, twoResponse.StatusCode);
            var two = (await twoResponse.Content.ReadFromJsonAsync<ProductComparisonResponseDto>())!;
            Assert.Equal(new[] { second.Id, first.Id }, two.Products.Select(p => p.Id));
            Assert.Equal(50000.50m, two.Products[0].Price);
            Assert.Equal(brand.Id, two.Products[0].Brand.Id);
            Assert.Equal(brand.Name, two.Products[0].Brand.Name);
            var spec = two.Products[0].Specification!;
            Assert.Equal("CPU B", spec.Processor);
            Assert.Equal("GPU B", spec.GPU);
            Assert.Equal(32, spec.RamGb);
            Assert.Equal(512, spec.StorageGb);
            Assert.Equal(15.6m, spec.ScreenSize);
            Assert.Equal("1920x1080", spec.Resolution);
            Assert.Equal(144, spec.RefreshRate);
            Assert.Equal(1.875m, spec.Weight);
            Assert.Equal("Test OS", spec.OperatingSystem);

            using var json = JsonDocument.Parse(await twoResponse.Content.ReadAsStringAsync());
            var item = json.RootElement.GetProperty("products")[0];
            Assert.Equal(new[] { "brand", "id", "name", "price", "specification" }, item.EnumerateObject().Select(p => p.Name).Order());
            Assert.Equal(new[] { "id", "name" }, item.GetProperty("brand").EnumerateObject().Select(p => p.Name).Order());
            var three = (await client.GetFromJsonAsync<ProductComparisonResponseDto>(
                $"/api/products/compare?ids={missingSpec.Id}&ids={first.Id}&ids={second.Id}"))!;
            Assert.Equal(new[] { missingSpec.Id, first.Id, second.Id }, three.Products.Select(p => p.Id));
            Assert.Null(three.Products[0].Specification);
            Assert.DoesNotContain(three.Products, p => p.Id == unselected.Id);

            using var missing = await client.GetAsync($"/api/products/compare?ids={first.Id}&ids=2147483647&ids=2147483646");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal("application/problem+json", missing.Content.Headers.ContentType!.MediaType);
            using var error = JsonDocument.Parse(await missing.Content.ReadAsStringAsync());
            Assert.Equal(new[] { int.MaxValue, int.MaxValue - 1 },
                error.RootElement.GetProperty("missingProductIds").EnumerateArray().Select(id => id.GetInt32()));
            Assert.False(error.RootElement.TryGetProperty("products", out _));
            Assert.True(error.RootElement.TryGetProperty("traceId", out _));

            var recorder = new QueryRecorder();
            var recordedOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).AddInterceptors(recorder).Options;
            await using var recordedContext = new ApplicationDbContext(recordedOptions);
            var loaded = await new ProductRepository(recordedContext).GetByIdsAsync([second.Id, first.Id, missingSpec.Id]);
            Assert.Equal(3, loaded.Count);
            Assert.Single(recorder.Commands);
            Assert.Contains("WHERE", recorder.Commands[0]);
            Assert.Empty(recordedContext.ChangeTracker.Entries());
            Assert.All(loaded, p => Assert.Equal(brand.Name, p.Brand.Name));
            Assert.Equal("CPU B", loaded.Single(p => p.Id == second.Id).LaptopSpecification!.Processor);
        }
        finally
        {
            database.Products.RemoveRange(products);
            await database.SaveChangesAsync();
            database.RemoveRange(brand, category);
            await database.SaveChangesAsync();
        }
    }

    private sealed class QueryRecorder : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
