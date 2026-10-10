using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Pikwise.Application.Products.DTOs;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.IntegrationTests;

public class TextCollationSqlTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Text_columns_use_latin1_so_lowercase_search_matches_regardless_of_server_default()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")!;
        Assert.Equal("PikwiseSession3Tests", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var database = new ApplicationDbContext(options);
        await database.Database.MigrateAsync();

        // Every text column uses the model's collation, not the database default (ADR-030).
        var collations = await database.Database.SqlQueryRaw<string>(
            """
            SELECT c.collation_name AS Value
            FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
            WHERE c.collation_name IS NOT NULL AND t.name <> '__EFMigrationsHistory'
            """).ToListAsync();
        Assert.NotEmpty(collations);
        Assert.All(collations, collation => Assert.Contains(collation,
            new[] { ApplicationDbContext.DefaultTextCollation, "Latin1_General_100_BIN2" }));

        var suffix = Guid.NewGuid().ToString("N");
        var brand = new Brand { Name = "Collation brand " + suffix };
        var category = new Category { Name = "Collation category " + suffix };
        var product = new Product
        {
            Name = "Collation laptop " + suffix, Price = 30000m, Stock = 1, IsActive = true,
            Brand = brand, Category = category, CreatedAt = DateTimeOffset.UtcNow,
            LaptopSpecification = new LaptopSpecification { Processor = "Intel Core Ultra 7 155H", GPU = "Intel Arc Graphics" }
        };
        database.Add(product);
        await database.SaveChangesAsync();
        try
        {
            await using var factory = new PikwiseApiFactory(connection);
            using var client = factory.CreateClient();
            // Under Turkish_CI_AS "intel" does not equal "Intel" (Turkish i/I casing); under Latin1 it does.
            var page = (await client.GetFromJsonAsync<PagedProductResponseDto>(
                $"/api/products?brandId={brand.Id}&cpu=intel&gpu=intel%20arc"))!;

            Assert.Equal(product.Id, Assert.Single(page.Items).Id);
        }
        finally
        {
            database.Products.Remove(product);
            await database.SaveChangesAsync();
            database.RemoveRange(brand, category);
            await database.SaveChangesAsync();
        }
    }
}
