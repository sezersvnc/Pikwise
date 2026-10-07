using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;
using Pikwise.Infrastructure.Recommendations;

namespace Pikwise.IntegrationTests;

public class RecommendationSqlTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Recommend_ranks_stored_laptops_and_loads_candidates_in_one_untracked_query()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")!;
        Assert.Equal("PikwiseSession3Tests", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var database = new ApplicationDbContext(options);
        await database.Database.MigrateAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var brand = new Brand { Name = "Recommendation brand " + suffix };
        var category = new Category { Name = "Recommendation category " + suffix };
        // Prices below 1.00 isolate these rows from other test data through the budget constraint.
        Product Laptop(string name, decimal price, int ram, decimal? weight, bool isActive = true, int stock = 5) => new()
        {
            Name = name + " " + suffix, Price = price, Stock = stock, IsActive = isActive,
            Brand = brand, Category = category, CreatedAt = DateTimeOffset.UtcNow,
            LaptopSpecification = new LaptopSpecification
            {
                Processor = "AMD Ryzen AI 7 PRO 450", GPU = "AMD Radeon 860M", RamGb = ram, StorageGb = 1000,
                RefreshRate = 120, Weight = weight, Resolution = "1920x1200", ScreenSize = 14m, OperatingSystem = "Test OS"
            }
        };
        var best = Laptop("Best", 0.90m, 32, 1.2m);
        var second = Laptop("Second", 0.50m, 16, 1.2m);
        var unknownWeight = Laptop("Unknown weight", 0.40m, 16, null);
        var fourth = Laptop("Fourth", 0.30m, 8, 2.4m);
        var inactive = Laptop("Inactive", 0.20m, 64, 1.0m, isActive: false);
        var noStock = Laptop("No stock", 0.20m, 64, 1.0m, stock: 0);
        var missingSpecification = Laptop("No specification", 0.10m, 64, 1.0m);
        missingSpecification.LaptopSpecification = null;
        Product[] products = [best, second, unknownWeight, fourth, inactive, noStock, missingSpecification];
        database.AddRange(products);
        await database.SaveChangesAsync();
        try
        {
            await using var factory = new PikwiseApiFactory(connection);
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync("/api/recommendations",
                new { budgetMax = 0.99m, importance = new { weight = 5 } });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = (await response.Content.ReadFromJsonAsync<RecommendationResponseDto>())!;

            Assert.Equal(new[] { best.Id, second.Id, unknownWeight.Id }, result.Items.Select(item => item.ProductId));
            Assert.Equal(new[] { 1, 2, 3 }, result.Items.Select(item => item.Rank));
            Assert.True(result.Items[0].Score > result.Items[1].Score);
            Assert.Equal(new[] { "weight" }, result.Items[2].UnknownCriteria);
            Assert.Equal(15, result.Items[2].KnownImportance);
            Assert.Equal(20, result.Items[2].TotalImportance);
            Assert.Equal(brand.Name, result.Items[0].Brand.Name);
            Assert.Equal(0.90m, result.Items[0].Price);
            Assert.Equal("AMD Ryzen AI 7 PRO 450", result.Items[0].Specification!.Processor);
            Assert.True(result.Summary.RemovedByEligibility >= 2);
            Assert.True(result.Summary.RankedCount >= 4);
            // Best and Second differ by 10 points (RAM), so no cheaper product is near-equal.
            Assert.Equal(best.Id, result.ValueAnalysis!.BestFitProductId);
            Assert.Equal(best.Id, result.ValueAnalysis.BestValueProductId);
            Assert.False(result.ValueAnalysis.IsSmallGainUpgrade);
            Assert.Empty(result.ValueAnalysis.CheaperAlternatives);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(new[] { "items", "summary", "valueAnalysis" }, json.RootElement.EnumerateObject().Select(p => p.Name));
            Assert.Equal(new[] { "bestFitProductId", "bestValueProductId", "cheaperAlternatives", "isSmallGainUpgrade", "nearEqualScoreGap" },
                json.RootElement.GetProperty("valueAnalysis").EnumerateObject().Select(p => p.Name).Order());
            var item = json.RootElement.GetProperty("items")[0];
            Assert.Equal(new[] { "brand", "components", "knownImportance", "knownWeightShare", "name", "price", "productId",
                "rank", "score", "specification", "totalImportance", "unknownCriteria" }, item.EnumerateObject().Select(p => p.Name).Order());
            Assert.Equal(new[] { "contribution", "criterion", "normalizedValue", "value", "weight" },
                item.GetProperty("components")[0].EnumerateObject().Select(p => p.Name).Order());

            var recorder = new QueryRecorder();
            var recordedOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).AddInterceptors(recorder).Options;
            await using var recordedContext = new ApplicationDbContext(recordedOptions);
            var candidates = await new RecommendationRepository(recordedContext).GetCandidatesAsync();
            Assert.Single(recorder.Commands);
            Assert.Empty(recordedContext.ChangeTracker.Entries());
            Assert.All(products, p => Assert.Contains(candidates, c => c.Id == p.Id));
            Assert.Equal(brand.Name, candidates.Single(c => c.Id == best.Id).Brand.Name);
            Assert.Null(candidates.Single(c => c.Id == missingSpecification.Id).LaptopSpecification);
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
