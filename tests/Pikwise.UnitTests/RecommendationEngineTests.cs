using Pikwise.Application.Recommendations.Models;
using Pikwise.Application.Recommendations.Scoring;
using Pikwise.Domain.Entities;

namespace Pikwise.UnitTests;

public class RecommendationEngineTests
{
    // Sample user of the Session 11 hand calculation (RECOMMENDATION_ENGINE.md).
    private static readonly UserRequirements SampleUser = new()
    {
        BudgetMax = 70000m,
        MinRamGb = 16,
        Importance = new ImportanceLevels(Ram: 3, Storage: 3, Cpu: 4, Gpu: 2, Weight: 5, RefreshRate: 1)
    };

    [Fact]
    public void Real_dataset_reproduces_the_hand_verified_ranking()
    {
        var result = RecommendationEngine.Recommend(RecommendationTestData.Session115Laptops(), SampleUser);

        Assert.Equal(25, result.CandidateCount);
        Assert.Equal(2, result.RemovedByEligibility);
        Assert.Equal(6, result.RemovedByConstraints);
        Assert.Equal(0, result.RemovedByKnownShare);
        Assert.Equal(17, result.Ranked.Count);
        Assert.Equal(new[] { "DELL PW514265", "DELL PW516265", "Fujitsu UH90/J3" }, result.Top.Select(item => item.Product.Name));
        Assert.Equal(new[] { 75.41m, 66.33m, 64.71m, 63.37m }, result.Ranked.Take(4).Select(item => item.Score));
        Assert.Equal("GIGABYTE AERO X16 1VH93NEC94AH", result.Ranked[3].Product.Name);

        var first = result.Ranked[0];
        Assert.Equal(18, first.KnownImportance);
        Assert.Empty(first.UnknownCriteria);
        var fujitsu = result.Ranked[2];
        Assert.Equal(17, fujitsu.KnownImportance);
        Assert.Equal(new[] { RecommendationCriterion.RefreshRate }, fujitsu.UnknownCriteria);
    }

    [Fact]
    public void Real_dataset_ranks_EliteBook_without_cpu_and_gpu_data_fifth_on_known_criteria_only()
    {
        // Agreed missing-data rule: no guess and no penalty; the known share is reported.
        var result = RecommendationEngine.Recommend(RecommendationTestData.Session115Laptops(), SampleUser);

        var eliteBook = result.Ranked[4];
        Assert.Equal("HP EliteBook 6 G1i 14 AI PC", eliteBook.Product.Name);
        Assert.Equal(12, eliteBook.KnownImportance);
        Assert.Equal(18, eliteBook.TotalImportance);
        Assert.Equal(0.6667m, eliteBook.KnownWeightShare);
        Assert.Equal(new[] { RecommendationCriterion.Cpu, RecommendationCriterion.Gpu }, eliteBook.UnknownCriteria);
        Assert.Equal(new[] { RecommendationCriterion.Ram, RecommendationCriterion.Storage, RecommendationCriterion.Weight,
            RecommendationCriterion.RefreshRate }, eliteBook.Components.Select(component => component.Criterion));
        // Weights are rescaled over the known 12 levels: weight importance 5/12.
        Assert.Equal(0.4167m, eliteBook.Components.Single(c => c.Criterion == RecommendationCriterion.Weight).Weight);
        Assert.DoesNotContain(result.Top, item => item.Product.Name == eliteBook.Product.Name);
    }

    [Fact]
    public void Real_dataset_components_explain_rank_one()
    {
        var first = RecommendationEngine.Recommend(RecommendationTestData.Session115Laptops(), SampleUser).Ranked[0];

        var expected = new (RecommendationCriterion, decimal Value, decimal Normalized, decimal Weight, decimal Contribution)[]
        {
            (RecommendationCriterion.Ram, 32m, 1m, 0.1667m, 16.6667m),
            (RecommendationCriterion.Storage, 1000m, 0.9688m, 0.1667m, 16.1458m),
            (RecommendationCriterion.Cpu, 4m, 0.75m, 0.2222m, 16.6667m),
            (RecommendationCriterion.Gpu, 3m, 0.5m, 0.1111m, 5.5556m),
            (RecommendationCriterion.Weight, 1.4m, 0.7333m, 0.2778m, 20.3704m),
            (RecommendationCriterion.RefreshRate, 60m, 0m, 0.0556m, 0m)
        };
        Assert.Equal(expected, first.Components.Select(c => (c.Criterion, c.Value, c.NormalizedValue, c.Weight, c.Contribution)));
        Assert.Equal(first.Score, decimal.Round(first.Components.Sum(c => c.Contribution), 2));
    }

    [Fact]
    public void Same_input_in_any_order_produces_the_same_ranking()
    {
        var laptops = RecommendationTestData.Session115Laptops();
        var baseline = Describe(RecommendationEngine.Recommend(laptops, SampleUser));
        var reversed = Describe(RecommendationEngine.Recommend(laptops.Reverse(), SampleUser));
        var shuffled = Describe(RecommendationEngine.Recommend(laptops.OrderBy(p => p.Name.Length).ThenByDescending(p => p.Id), SampleUser));

        Assert.Equal(baseline, reversed);
        Assert.Equal(baseline, shuffled);
        Assert.Equal(baseline, Describe(RecommendationEngine.Recommend(laptops, SampleUser)));
    }

    [Fact]
    public void Inactive_and_zero_stock_products_are_not_recommended()
    {
        var inactive = Laptop(1);
        inactive.IsActive = false;
        var noStock = Laptop(2);
        noStock.Stock = 0;
        var result = RecommendationEngine.Recommend([inactive, noStock, Laptop(3)], new UserRequirements());

        Assert.Equal(new[] { 3 }, result.Ranked.Select(item => item.Product.Id));
        Assert.Equal(2, result.RemovedByEligibility);
    }

    [Fact]
    public void Hard_constraints_are_strict_and_inclusive_at_the_boundary()
    {
        var atLimits = Laptop(1, price: 50000m, ram: 16, storage: 512, weight: 1.5m);
        var overBudget = Laptop(2, price: 50000.01m);
        var lowRam = Laptop(3, ram: 15);
        var lowStorage = Laptop(4, storage: 511);
        var heavy = Laptop(5, weight: 1.501m);
        var requirements = new UserRequirements { BudgetMax = 50000m, MinRamGb = 16, MinStorageGb = 512, MaxWeightKg = 1.5m };

        var result = RecommendationEngine.Recommend([atLimits, overBudget, lowRam, lowStorage, heavy], requirements);

        Assert.Equal(new[] { 1 }, result.Ranked.Select(item => item.Product.Id));
        Assert.Equal(4, result.RemovedByConstraints);
    }

    [Theory]
    [InlineData("ram")]
    [InlineData("storage")]
    [InlineData("weight")]
    public void Unknown_field_of_an_active_constraint_removes_the_product(string field)
    {
        var product = Laptop(1);
        if (field == "ram") product.LaptopSpecification!.RamGb = null;
        if (field == "storage") product.LaptopSpecification!.StorageGb = null;
        if (field == "weight") product.LaptopSpecification!.Weight = null;
        var requirements = new UserRequirements { MinRamGb = 1, MinStorageGb = 1, MaxWeightKg = 10m };

        var result = RecommendationEngine.Recommend([product], requirements);

        Assert.Empty(result.Ranked);
        Assert.Equal(1, result.RemovedByConstraints);
    }

    [Fact]
    public void Unknown_field_without_an_active_constraint_is_only_left_out_of_the_score()
    {
        var product = Laptop(1);
        product.LaptopSpecification!.Weight = null;

        var scored = Assert.Single(RecommendationEngine.Recommend([product], new UserRequirements { MinRamGb = 8 }).Ranked);

        Assert.Equal(new[] { RecommendationCriterion.Weight }, scored.UnknownCriteria);
        Assert.Equal(15, scored.KnownImportance);
    }

    [Fact]
    public void Missing_specification_fails_spec_constraints_and_has_no_known_share()
    {
        var withoutSpecification = Laptop(1);
        withoutSpecification.LaptopSpecification = null;

        var constrained = RecommendationEngine.Recommend([withoutSpecification], new UserRequirements { MinRamGb = 8 });
        var budgetOnly = RecommendationEngine.Recommend([withoutSpecification], new UserRequirements { BudgetMax = 100000m });

        Assert.Equal(1, constrained.RemovedByConstraints);
        Assert.Equal(0, budgetOnly.RemovedByConstraints);
        Assert.Equal(1, budgetOnly.RemovedByKnownShare);
        Assert.Empty(budgetOnly.Ranked);
    }

    [Theory]
    // RAM 8..32, storage 256..1024, refresh 60..165: values are clamped before mapping to 0..1.
    [InlineData(4, 128, 30, 0)]
    [InlineData(8, 256, 60, 0)]
    [InlineData(20, 640, 165, 1)]
    [InlineData(64, 2000, 240, 1)]
    public void Higher_is_better_values_are_clamped_to_the_reference_range(int ram, int storage, int refreshRate, int refreshNormalized)
    {
        var product = Laptop(1, ram: ram, storage: storage, refreshRate: refreshRate);

        var scored = RecommendationEngine.Recommend([product], new UserRequirements()).Ranked.Single();

        Assert.Equal(Math.Clamp((ram - 8) / 24m, 0, 1), Normalized(scored, RecommendationCriterion.Ram), 4);
        Assert.Equal(Math.Clamp((storage - 256) / 768m, 0, 1), Normalized(scored, RecommendationCriterion.Storage), 4);
        Assert.Equal(refreshNormalized, Normalized(scored, RecommendationCriterion.RefreshRate));
    }

    [Theory]
    // Weight is lower-is-better over 1.0..2.5 kg.
    [InlineData(0.8, 1)]
    [InlineData(1.0, 1)]
    [InlineData(1.75, 0.5)]
    [InlineData(2.5, 0)]
    [InlineData(3.2, 0)]
    public void Weight_is_inverted_and_clamped(double weight, double expected)
    {
        var product = Laptop(1, weight: (decimal)weight);

        var scored = RecommendationEngine.Recommend([product], new UserRequirements()).Ranked.Single();

        Assert.Equal((decimal)expected, Normalized(scored, RecommendationCriterion.Weight));
    }

    [Theory]
    [InlineData("Intel Core i3-1305U", "Intel UHD Graphics", 0, 0)]
    [InlineData("AMD Ryzen 7 170", "Intel Arc Graphics", 0.5, 0.5)]
    [InlineData("Intel Core Ultra 9 386H", "NVIDIA GeForce RTX 5080 Laptop GPU", 1, 1)]
    public void Tiers_map_to_tier_minus_one_over_four(string processor, string gpu, double cpu, double gpuExpected)
    {
        var product = Laptop(1, processor: processor, gpu: gpu);

        var scored = RecommendationEngine.Recommend([product], new UserRequirements()).Ranked.Single();

        Assert.Equal((decimal)cpu, Normalized(scored, RecommendationCriterion.Cpu));
        Assert.Equal((decimal)gpuExpected, Normalized(scored, RecommendationCriterion.Gpu));
    }

    [Fact]
    public void Unknown_criteria_are_left_out_and_known_weights_are_rescaled()
    {
        // RAM 32 (n=1), storage 256 (n=0); CPU, GPU, weight and refresh rate unknown.
        var product = Laptop(1, ram: 32, storage: 256, processor: "Unknown CPU", gpu: "Intel Graphics", weight: null, refreshRate: null);
        var requirements = new UserRequirements { Importance = new ImportanceLevels(Ram: 3, Storage: 1, Cpu: 1, Gpu: 1, Weight: 1, RefreshRate: 1) };

        var scored = RecommendationEngine.Recommend([product], requirements).Ranked.Single();

        // Known share 4/8 is exactly 50%, so the product is still recommended; RAM weight 3/4.
        Assert.Equal(75m, scored.Score);
        Assert.Equal(4, scored.KnownImportance);
        Assert.Equal(8, scored.TotalImportance);
        Assert.Equal(0.5m, scored.KnownWeightShare);
        Assert.Equal(new[] { 0.75m, 0.25m }, scored.Components.Select(c => c.Weight));
        Assert.Equal(new[] { RecommendationCriterion.Cpu, RecommendationCriterion.Gpu, RecommendationCriterion.Weight,
            RecommendationCriterion.RefreshRate }, scored.UnknownCriteria);
    }

    [Fact]
    public void Known_share_below_fifty_percent_is_not_recommended()
    {
        // With default levels, RAM + storage + weight known is 9/18 (kept); RAM + storage is 6/18 (removed).
        var half = Laptop(1, processor: null, gpu: null, refreshRate: null);
        var third = Laptop(2, processor: null, gpu: null, refreshRate: null, weight: null);

        var result = RecommendationEngine.Recommend([half, third], new UserRequirements());

        Assert.Equal(new[] { 1 }, result.Ranked.Select(item => item.Product.Id));
        Assert.Equal(1, result.RemovedByKnownShare);
    }

    [Fact]
    public void Score_is_rounded_half_up_to_two_decimals()
    {
        // Only weight contributes: n = 0.7407 and the score is 100 * 0.7407 / 6 = 12.345 exactly.
        var product = Laptop(1, ram: 8, storage: 256, processor: "Intel Core i3-1305U", gpu: "Intel UHD Graphics",
            weight: 1.38895m, refreshRate: 60);

        var scored = RecommendationEngine.Recommend([product], new UserRequirements()).Ranked.Single();

        Assert.Equal(12.35m, scored.Score);
    }

    [Fact]
    public void Equal_rounded_scores_are_ordered_by_price_then_id()
    {
        // 1.4 kg (45.3571) and 1.4001 kg (45.3560) differ before rounding; both round to 45.36.
        var expensive = Laptop(1, price: 60000m, weight: 1.4m);
        var cheapHigherId = Laptop(3, price: 40000m, weight: 1.4001m);
        var cheapLowerId = Laptop(2, price: 40000m, weight: 1.4001m);
        var best = Laptop(4, price: 90000m, weight: 1.0m);

        var result = RecommendationEngine.Recommend([expensive, cheapHigherId, cheapLowerId, best], new UserRequirements());

        Assert.Equal(new[] { 4, 2, 3, 1 }, result.Ranked.Select(item => item.Product.Id));
        Assert.Equal(new[] { 45.36m, 45.36m, 45.36m }, result.Ranked.Skip(1).Select(item => item.Score));
        Assert.Equal(new[] { 4, 2, 3 }, result.Top.Select(item => item.Product.Id));
    }

    [Fact]
    public void Fewer_than_three_qualifying_products_returns_only_those()
    {
        Assert.Single(RecommendationEngine.Recommend([Laptop(1)], new UserRequirements()).Top);
        Assert.Empty(RecommendationEngine.Recommend([], new UserRequirements()).Top);
    }

    private static decimal Normalized(ScoredProduct scored, RecommendationCriterion criterion) =>
        scored.Components.Single(component => component.Criterion == criterion).NormalizedValue;

    private static string Describe(RecommendationResult result) => string.Join(";", result.Ranked.Select(item =>
        $"{item.Product.Id}:{item.Score}:{item.KnownImportance}:" +
        string.Join(",", item.Components.Select(c => $"{c.Criterion}={c.Value}/{c.NormalizedValue}/{c.Weight}/{c.Contribution}"))));

    private static Product Laptop(int id, decimal price = 50000m, int? ram = 16, int? storage = 512,
        string? processor = "AMD Ryzen 7 170", string? gpu = "AMD Radeon 680M", decimal? weight = 1.5m, int? refreshRate = 120) => new()
    {
        Id = id, Name = "Laptop " + id, Price = price, Stock = 5, IsActive = true,
        Brand = new Brand { Id = 1, Name = "Test brand" },
        LaptopSpecification = new LaptopSpecification
        {
            Processor = processor, GPU = gpu, RamGb = ram, StorageGb = storage, Weight = weight, RefreshRate = refreshRate
        }
    };
}
