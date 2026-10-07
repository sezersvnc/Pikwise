using Pikwise.Application.Recommendations.DTOs;
using Pikwise.Application.Recommendations.Exceptions;
using Pikwise.Application.Recommendations.Interfaces;
using Pikwise.Application.Recommendations.Services;
using Pikwise.Domain.Entities;

namespace Pikwise.UnitTests;

public class RecommendationServiceTests
{
    [Fact]
    public async Task Recommend_returns_top_three_with_explanations_and_forwards_cancellation()
    {
        var repository = new CandidateRepository(RecommendationTestData.Session115Laptops());
        using var cancellation = new CancellationTokenSource();
        var request = new RecommendationRequestDto
        {
            BudgetMax = 70000m,
            MinRamGb = 16,
            Importance = new RecommendationImportanceDto { Ram = 3, Storage = 3, Cpu = 4, Gpu = 2, Weight = 5, RefreshRate = 1 }
        };

        var response = await new RecommendationService(repository).RecommendAsync(request, cancellation.Token);

        Assert.Equal(new[] { 1, 2, 3 }, response.Items.Select(item => item.Rank));
        Assert.Equal(new[] { 13, 3, 16 }, response.Items.Select(item => item.ProductId));
        Assert.Equal(new[] { 75.41m, 66.33m, 64.71m }, response.Items.Select(item => item.Score));
        var first = response.Items[0];
        Assert.Equal("DELL PW514265", first.Name);
        Assert.Equal(68000m, first.Price);
        Assert.Equal("DELL", first.Brand.Name);
        Assert.Equal("AMD Ryzen AI 7 PRO 450", first.Specification!.Processor);
        Assert.Equal(new[] { "ram", "storage", "cpu", "gpu", "weight", "refreshRate" }, first.Components.Select(c => c.Criterion));
        Assert.Equal(1m, first.KnownWeightShare);
        var third = response.Items[2];
        Assert.Equal(new[] { "refreshRate" }, third.UnknownCriteria);
        Assert.Equal(17, third.KnownImportance);
        Assert.Equal(18, third.TotalImportance);
        Assert.Equal(0.9444m, third.KnownWeightShare);
        Assert.Equal(new RecommendationSummaryDto(25, 2, 6, 0, 17), response.Summary);
        var value = response.ValueAnalysis!;
        Assert.Equal(13, value.BestFitProductId);
        Assert.Equal(13, value.BestValueProductId);
        Assert.False(value.IsSmallGainUpgrade);
        Assert.Equal(3m, value.NearEqualScoreGap);
        Assert.Empty(value.CheaperAlternatives);
        Assert.Equal(1, repository.CallCount);
        Assert.Equal(cancellation.Token, repository.Cancellation);
    }

    [Fact]
    public async Task Value_analysis_maps_a_cheaper_near_equal_alternative_outside_the_top_three()
    {
        // With default importance, 1.0 kg scores 49.80 and 1.2 kg scores 47.58 (2.22 points lower).
        Product Laptop(int id, decimal price, decimal weight) => new()
        {
            Id = id, Name = "Laptop " + id, Price = price, Stock = 5, IsActive = true,
            Brand = new Brand { Id = 1, Name = "Test brand" },
            LaptopSpecification = new LaptopSpecification
            {
                Processor = "AMD Ryzen 7 170", GPU = "AMD Radeon 680M", RamGb = 16, StorageGb = 512, Weight = weight, RefreshRate = 120
            }
        };
        var repository = new CandidateRepository(
            [Laptop(1, 60000m, 1.0m), Laptop(2, 70000m, 1.05m), Laptop(3, 70000m, 1.1m), Laptop(4, 50000m, 1.2m)]);

        var response = await new RecommendationService(repository).RecommendAsync(new RecommendationRequestDto());

        Assert.Equal(new[] { 1, 2, 3 }, response.Items.Select(item => item.ProductId));
        var value = response.ValueAnalysis!;
        Assert.Equal(1, value.BestFitProductId);
        Assert.Equal(4, value.BestValueProductId);
        Assert.True(value.IsSmallGainUpgrade);
        Assert.Equal(new CheaperAlternativeDto(4, 4, "Laptop 4", 50000m, 47.58m, 2.22m, 10000m, 4504.50m),
            Assert.Single(value.CheaperAlternatives));
    }

    [Fact]
    public async Task No_qualifying_product_returns_null_value_analysis()
    {
        var response = await new RecommendationService(new CandidateRepository([])).RecommendAsync(new RecommendationRequestDto());

        Assert.Empty(response.Items);
        Assert.Null(response.ValueAnalysis);
    }

    [Fact]
    public async Task Absent_importance_levels_default_to_three()
    {
        var repository = new CandidateRepository(RecommendationTestData.Session115Laptops());
        var explicitDefaults = new RecommendationRequestDto
        {
            Importance = new RecommendationImportanceDto { Ram = 3, Storage = 3, Cpu = 3, Gpu = 3, Weight = 3, RefreshRate = 3 }
        };

        var withDefaults = await new RecommendationService(repository).RecommendAsync(new RecommendationRequestDto());
        var withExplicit = await new RecommendationService(repository).RecommendAsync(explicitDefaults);

        Assert.Equal(withExplicit.Items, withDefaults.Items, (a, b) => a.ProductId == b.ProductId && a.Score == b.Score);
        Assert.All(withDefaults.Items, item => Assert.Equal(18, item.TotalImportance));
        Assert.Equal(new[] { 0.1667m }, withDefaults.Items[0].Components.Select(c => c.Weight).Distinct());
    }

    public static TheoryData<RecommendationRequestDto, string> InvalidRequests => new()
    {
        { new RecommendationRequestDto { BudgetMax = 0m }, "BudgetMax" },
        { new RecommendationRequestDto { BudgetMax = -1m }, "BudgetMax" },
        { new RecommendationRequestDto { BudgetMax = 10000000.01m }, "BudgetMax" },
        { new RecommendationRequestDto { BudgetMax = 100.001m }, "BudgetMax" },
        { new RecommendationRequestDto { MinRamGb = 0 }, "MinRamGb" },
        { new RecommendationRequestDto { MinRamGb = 257 }, "MinRamGb" },
        { new RecommendationRequestDto { MinStorageGb = 0 }, "MinStorageGb" },
        { new RecommendationRequestDto { MinStorageGb = 16385 }, "MinStorageGb" },
        { new RecommendationRequestDto { MaxWeightKg = 0.09m }, "MaxWeightKg" },
        { new RecommendationRequestDto { MaxWeightKg = 10.01m }, "MaxWeightKg" },
        { new RecommendationRequestDto { MaxWeightKg = 1.555m }, "MaxWeightKg" },
        { new RecommendationRequestDto { Importance = new RecommendationImportanceDto { Ram = 0 } }, "Importance.Ram" },
        { new RecommendationRequestDto { Importance = new RecommendationImportanceDto { Storage = 6 } }, "Importance.Storage" },
        { new RecommendationRequestDto { Importance = new RecommendationImportanceDto { Cpu = -1 } }, "Importance.Cpu" },
        { new RecommendationRequestDto { Importance = new RecommendationImportanceDto { Gpu = 6 } }, "Importance.Gpu" },
        { new RecommendationRequestDto { Importance = new RecommendationImportanceDto { Weight = 0 } }, "Importance.Weight" },
        { new RecommendationRequestDto { Importance = new RecommendationImportanceDto { RefreshRate = 10 } }, "Importance.RefreshRate" }
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Invalid_requests_are_rejected_before_data_access(RecommendationRequestDto request, string field)
    {
        var repository = new CandidateRepository([]);

        var exception = await Assert.ThrowsAsync<RecommendationValidationException>(
            () => new RecommendationService(repository).RecommendAsync(request));

        Assert.Equal(new[] { field }, exception.Errors.Keys);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task Boundary_values_are_accepted()
    {
        var repository = new CandidateRepository([]);
        var request = new RecommendationRequestDto
        {
            BudgetMax = 10000000m, MinRamGb = 256, MinStorageGb = 16384, MaxWeightKg = 0.1m,
            Importance = new RecommendationImportanceDto { Ram = 1, Storage = 5, Cpu = 1, Gpu = 5, Weight = 1, RefreshRate = 5 }
        };

        var response = await new RecommendationService(repository).RecommendAsync(request);

        Assert.Empty(response.Items);
        Assert.Equal(1, repository.CallCount);
    }

    private sealed class CandidateRepository(IReadOnlyList<Product> products) : IRecommendationRepository
    {
        public int CallCount { get; private set; }
        public CancellationToken Cancellation { get; private set; }

        public Task<IReadOnlyList<Product>> GetCandidatesAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            Cancellation = cancellationToken;
            return Task.FromResult(products);
        }
    }
}
