using Pikwise.Application.Recommendations.Models;
using Pikwise.Application.Recommendations.Scoring;
using Pikwise.Domain.Entities;

namespace Pikwise.UnitTests;

public class ValueAnalyzerTests
{
    // Products differ only in weight and price. With default importance every 0.27 kg of extra
    // weight costs exactly 3.00 score points: 1.0 kg scores 49.80, 1.27 kg 46.80, 1.271 kg 46.79.

    [Fact]
    public void No_ranked_product_means_no_value_analysis()
    {
        Assert.Null(Analyze());
    }

    [Fact]
    public void Single_product_is_both_best_fit_and_best_value()
    {
        var value = Analyze(Laptop(1, 60000m, 1.0m))!;

        Assert.Equal(1, value.BestFit.Product.Id);
        Assert.Equal(1, value.BestValue.Product.Id);
        Assert.Empty(value.CheaperAlternatives);
        Assert.False(value.IsSmallGainUpgrade);
    }

    [Fact]
    public void Cheaper_product_within_three_points_is_a_near_equal_alternative()
    {
        var value = Analyze(Laptop(1, 60000m, 1.0m), Laptop(2, 51000m, 1.27m))!;

        var alternative = Assert.Single(value.CheaperAlternatives);
        Assert.Equal(2, alternative.Product.Product.Id);
        Assert.Equal(2, alternative.Rank);
        Assert.Equal(49.80m, value.BestFit.Score);
        Assert.Equal(46.80m, alternative.Product.Score);
        Assert.Equal(3.00m, alternative.ScoreGap);
        Assert.Equal(9000m, alternative.PriceDifference);
        Assert.Equal(3000.00m, alternative.PricePerPoint);
        Assert.True(value.IsSmallGainUpgrade);
        Assert.Equal(2, value.BestValue.Product.Id);
    }

    [Fact]
    public void Cheaper_product_just_over_three_points_is_not_an_alternative()
    {
        var result = RecommendationEngine.Recommend([Laptop(1, 60000m, 1.0m), Laptop(2, 30000m, 1.271m)], new UserRequirements());
        var value = ValueAnalyzer.Analyze(result)!;

        Assert.Equal(3.01m, result.Ranked[0].Score - result.Ranked[1].Score);
        Assert.Empty(value.CheaperAlternatives);
        Assert.False(value.IsSmallGainUpgrade);
        Assert.Equal(1, value.BestValue.Product.Id);
    }

    [Fact]
    public void More_expensive_lower_scored_product_is_not_an_alternative()
    {
        var value = Analyze(Laptop(1, 60000m, 1.0m), Laptop(2, 65000m, 1.1m))!;

        Assert.Empty(value.CheaperAlternatives);
        Assert.Equal(1, value.BestValue.Product.Id);
    }

    [Fact]
    public void Price_per_point_is_rounded_half_up_to_two_decimals()
    {
        // Gap 49.80 - 48.69 = 1.11 points for 1,000.00: 900.9009... per point.
        var value = Analyze(Laptop(1, 60000m, 1.0m), Laptop(2, 59000m, 1.1m))!;

        var alternative = Assert.Single(value.CheaperAlternatives);
        Assert.Equal(1.11m, alternative.ScoreGap);
        Assert.Equal(900.90m, alternative.PricePerPoint);
    }

    [Fact]
    public void Alternative_outside_the_top_three_is_found_with_its_rank()
    {
        // Two pricier products rank 2 and 3; the cheap one ranks 4 and is still within 3 points.
        var value = Analyze(
            Laptop(1, 60000m, 1.0m), Laptop(2, 70000m, 1.05m), Laptop(3, 70000m, 1.1m), Laptop(4, 50000m, 1.2m))!;

        var alternative = Assert.Single(value.CheaperAlternatives);
        Assert.Equal(4, alternative.Product.Product.Id);
        Assert.Equal(4, alternative.Rank);
        Assert.Equal(2.22m, alternative.ScoreGap);
        Assert.Equal(4, value.BestValue.Product.Id);
    }

    [Fact]
    public void Best_value_is_cheapest_then_higher_score_then_lower_id()
    {
        // Ids 2 and 3 share the cheapest price; 2 has the higher score. Ids 4 and 5 tie completely.
        var byScore = Analyze(Laptop(1, 60000m, 1.0m), Laptop(2, 50000m, 1.1m), Laptop(3, 50000m, 1.2m))!;
        var byId = Analyze(Laptop(1, 60000m, 1.0m), Laptop(5, 50000m, 1.1m), Laptop(4, 50000m, 1.1m))!;

        Assert.Equal(2, byScore.BestValue.Product.Id);
        Assert.Equal(new[] { 2, 3 }, byScore.CheaperAlternatives.Select(a => a.Product.Product.Id));
        Assert.Equal(4, byId.BestValue.Product.Id);
        Assert.Equal(new[] { 4, 5 }, byId.CheaperAlternatives.Select(a => a.Product.Product.Id));
    }

    [Fact]
    public void Same_products_in_any_order_give_the_same_analysis()
    {
        Product[] products = [Laptop(1, 60000m, 1.0m), Laptop(2, 52000m, 1.2m), Laptop(3, 45000m, 1.25m), Laptop(4, 40000m, 1.5m)];

        var baseline = Describe(Analyze(products)!);

        Assert.Equal(baseline, Describe(Analyze(products.Reverse().ToArray())!));
        Assert.Equal(baseline, Describe(Analyze(products.OrderBy(p => p.Price).ToArray())!));
        Assert.Equal("best-fit=1;best-value=3;2@2:2.22/8000/3603.60;3@3:2.78/15000/5395.68", baseline);
    }

    [Fact]
    public void Real_dataset_sample_user_has_no_near_equal_cheaper_alternative()
    {
        // Rank 1 Dell PW514265 (75.41, 68,000); the best cheaper product scores 64.71, 10.70 points lower.
        var requirements = new UserRequirements
        {
            BudgetMax = 70000m,
            MinRamGb = 16,
            Importance = new ImportanceLevels(Ram: 3, Storage: 3, Cpu: 4, Gpu: 2, Weight: 5, RefreshRate: 1)
        };

        var value = ValueAnalyzer.Analyze(RecommendationEngine.Recommend(RecommendationTestData.Session115Laptops(), requirements))!;

        Assert.Equal(13, value.BestFit.Product.Id);
        Assert.Equal(13, value.BestValue.Product.Id);
        Assert.Empty(value.CheaperAlternatives);
        Assert.False(value.IsSmallGainUpgrade);
    }

    private static ValueAnalysis? Analyze(params Product[] products) =>
        ValueAnalyzer.Analyze(RecommendationEngine.Recommend(products, new UserRequirements()));

    // Invariant culture keeps the dot decimal separator on a Turkish-culture machine.
    private static string Describe(ValueAnalysis value) => FormattableString.Invariant(
        $"best-fit={value.BestFit.Product.Id};best-value={value.BestValue.Product.Id};") + string.Join(";",
            value.CheaperAlternatives.Select(a => FormattableString.Invariant(
                $"{a.Product.Product.Id}@{a.Rank}:{a.ScoreGap}/{a.PriceDifference:0}/{a.PricePerPoint}")));

    private static Product Laptop(int id, decimal price, decimal weight) => new()
    {
        Id = id, Name = "Laptop " + id, Price = price, Stock = 5, IsActive = true,
        Brand = new Brand { Id = 1, Name = "Test brand" },
        LaptopSpecification = new LaptopSpecification
        {
            Processor = "AMD Ryzen 7 170", GPU = "AMD Radeon 680M", RamGb = 16, StorageGb = 512,
            Weight = weight, RefreshRate = 120
        }
    };
}
