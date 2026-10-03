using System.Data.Common;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pikwise.Application.Products.DTOs;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;
using Pikwise.Infrastructure.Products;

namespace Pikwise.IntegrationTests;

public class ProductQuerySqlTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Catalog_combines_filters_sorts_pages_and_executes_pagination_in_sql()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")!;
        Assert.Equal("PikwiseSession3Tests", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var database = new ApplicationDbContext(options);
        await database.Database.MigrateAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var brand = new Brand { Name = "Query brand " + suffix };
        var other = new Brand { Name = "Query other " + suffix };
        var category = new Category { Name = "Query category " + suffix };
        Product Laptop(string name, decimal price, int ram = 16, int storage = 512, string cpu = "Core Test", string gpu = "RTX Test") => new()
        {
            Name = name, Price = price, Brand = brand, Category = category,
            CreatedAt = DateTimeOffset.UtcNow,
            LaptopSpecification = new LaptopSpecification { Processor = cpu, GPU = gpu, RamGb = ram, StorageGb = storage,
                ScreenSize = 15.6m, Resolution = "1920x1080", RefreshRate = 60, Weight = 2m, OperatingSystem = "Test OS" }
        };
        var first = Laptop("A", 30000);
        var second = Laptop("B", 30000);
        var third = Laptop("C", 45000, 32, 1024);
        var cheap = Laptop("Cheap", 10000);
        var expensive = Laptop("Expensive", 60000);
        var lowRam = Laptop("Low RAM", 30000, 8);
        var lowStorage = Laptop("Low storage", 30000, storage: 256);
        var otherCpu = Laptop("Other CPU", 30000, cpu: "Ryzen Test");
        var otherGpu = Laptop("Other GPU", 30000, gpu: "Integrated");
        var noSpec = Laptop("Missing specification", 30000);
        noSpec.LaptopSpecification = null;
        var otherBrand = Laptop("Other brand", 30000);
        otherBrand.Brand = other;
        Product[] products = [first, second, third, cheap, expensive, lowRam, lowStorage, otherCpu, otherGpu, noSpec, otherBrand];
        database.AddRange(products);
        await database.SaveChangesAsync();
        try
        {
            await using var factory = new PikwiseApiFactory(connection);
            using var client = factory.CreateClient();
            async Task<PagedProductResponseDto> Query(string parameters)
            {
                using var response = await client.GetAsync("/api/products?" + parameters);
                Assert.True(response.IsSuccessStatusCode, parameters + ": " + await response.Content.ReadAsStringAsync());
                return (await response.Content.ReadFromJsonAsync<PagedProductResponseDto>())!;
            }
            var all = await Query($"brandId={brand.Id}");
            Assert.Equal(10, all.TotalCount);
            Assert.Equal(20, all.PageSize);
            Assert.Equal(products.Where(p => p.Brand == brand).Select(p => p.Id).Order(), all.Items.Select(p => p.Id));
            Assert.Contains(all.Items, p => p.Id == noSpec.Id && p.Specification == null);
            Assert.DoesNotContain(all.Items, p => p.Id == otherBrand.Id);

            // Each individual constraint excludes its own counterexample before testing their intersection.
            foreach (var (filter, excluded) in new[] {
                ("minPrice=20000", cheap), ("maxPrice=50000", expensive), ("minRam=16", lowRam),
                ("minStorage=512", lowStorage), ("cpu=Core", otherCpu), ("gpu=RTX", otherGpu) })
            {
                var filtered = await Query($"brandId={brand.Id}&{filter}");
                Assert.Contains(filtered.Items, p => p.Id == first.Id);
                Assert.DoesNotContain(filtered.Items, p => p.Id == excluded.Id);
            }
            var combined = $"brandId={brand.Id}&minPrice=20000&maxPrice=50000&minRam=16&minStorage=512&cpu=%20Core%20&gpu=RTX";
            var eligible = new[] { first, second, third };
            foreach (var sort in new[] { "id", "price", "name", "ram", "createdAt" })
            foreach (var direction in new[] { "asc", "desc" })
            {
                Func<Product, IComparable> key = sort switch {
                    "price" => p => p.Price, "name" => p => p.Name, "ram" => p => p.LaptopSpecification!.RamGb,
                    "createdAt" => p => p.CreatedAt, _ => p => p.Id };
                var expected = (direction == "asc" ? eligible.OrderBy(key) : eligible.OrderByDescending(key)).ThenBy(p => p.Id);
                var sorted = await Query($"{combined}&sortBy={sort}&sortDirection={direction}");
                Assert.Equal(expected.Select(p => p.Id), sorted.Items.Select(p => p.Id));
                Assert.Equal(3, sorted.TotalCount);
            }
            var page1 = await Query($"{combined}&sortBy=PRICE&pageSize=1");
            var page2 = await Query($"{combined}&sortBy=price&pageSize=1&page=2");
            Assert.Equal(Math.Min(first.Id, second.Id), Assert.Single(page1.Items).Id);
            Assert.Equal(Math.Max(first.Id, second.Id), Assert.Single(page2.Items).Id);
            Assert.Equal(3, page2.TotalPages);
            Assert.True(page2.HasPreviousPage);
            Assert.True(page2.HasNextPage);
            var last = await Query($"{combined}&sortBy=price&pageSize=2&page=2");
            Assert.Equal(third.Id, Assert.Single(last.Items).Id);
            Assert.False(last.HasNextPage);
            var beyond = await Query($"{combined}&pageSize=2&page=3");
            Assert.Empty(beyond.Items);
            Assert.Equal(3, beyond.TotalCount);
            Assert.True(beyond.HasPreviousPage);
            Assert.False(beyond.HasNextPage);
            var empty = await Query($"brandId={brand.Id}&minPrice=60001&pageSize=100");
            Assert.Empty(empty.Items);
            Assert.Equal(0, empty.TotalPages);
            Assert.False(empty.HasPreviousPage);
            Assert.False(empty.HasNextPage);
            Assert.Empty((await Query("brandId=2147483647")).Items);

            var recorder = new QueryRecorder();
            var recordedOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).AddInterceptors(recorder).Options;
            await using var recordedContext = new ApplicationDbContext(recordedOptions);
            var result = await new ProductRepository(recordedContext).GetAllAsync(new ProductQueryRequestDto
            {
                BrandId = brand.Id, MinPrice = 20000, MaxPrice = 50000, MinRam = 16, MinStorage = 512,
                Cpu = "Core", Gpu = "RTX", SortBy = "price", Page = 2, PageSize = 1
            });
            Assert.Equal(3, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(2, recorder.Commands.Count);
            Assert.Contains("COUNT(*)", recorder.Commands[0]);
            Assert.Contains("WHERE", recorder.Commands[0]);
            Assert.Contains("OFFSET", recorder.Commands[1]);
            Assert.Contains("FETCH NEXT", recorder.Commands[1]);
        }
        finally
        {
            database.Products.RemoveRange(products);
            await database.SaveChangesAsync();
            database.RemoveRange(brand, other, category);
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
