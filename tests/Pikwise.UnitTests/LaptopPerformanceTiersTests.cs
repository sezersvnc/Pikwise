using Pikwise.Application.Recommendations.Scoring;

namespace Pikwise.UnitTests;

public class LaptopPerformanceTiersTests
{
    [Theory]
    [InlineData("Intel Core i3-1305U", 1)]
    [InlineData("Intel Core i7-1255U", 2)]
    [InlineData("Intel Core i7-1265U", 2)]
    [InlineData("Intel Core 5 120U", 2)]
    [InlineData("Qualcomm Snapdragon X1-26-100", 2)]
    [InlineData("Intel Core Ultra 5 325", 2)]
    [InlineData("AMD Ryzen 7 170", 3)]
    [InlineData("AMD Ryzen 5 7533HS", 3)]
    [InlineData("Intel Core Ultra 7 155U", 3)]
    [InlineData("Intel Core Ultra 7 355", 3)]
    [InlineData("AMD Ryzen AI 5 PRO 435", 3)]
    [InlineData("Intel Core Ultra 7 155H", 4)]
    [InlineData("Intel Core Ultra 7 366H", 4)]
    [InlineData("AMD Ryzen AI 7 350", 4)]
    [InlineData("AMD Ryzen AI 7 PRO 450", 4)]
    [InlineData("AMD Ryzen 7 260", 4)]
    [InlineData("Intel Core Ultra 9 386H", 5)]
    [InlineData("Intel Core 9 270H", 5)]
    [InlineData("Intel Core Ultra 7 255HX", 5)]
    public void Cpu_table_matches_the_approved_tiers(string processor, int tier) =>
        Assert.Equal(tier, LaptopPerformanceTiers.GetCpuTier(processor));

    [Theory]
    [InlineData("Intel UHD Graphics", 1)]
    [InlineData("Intel Iris Xe Graphics", 2)]
    [InlineData("AMD Radeon 680M", 2)]
    [InlineData("AMD Radeon 840M", 2)]
    [InlineData("AMD Radeon 860M", 3)]
    [InlineData("Intel Arc Graphics", 3)]
    [InlineData("NVIDIA GeForce RTX 4050", 4)]
    [InlineData("NVIDIA GeForce RTX 5050", 4)]
    [InlineData("NVIDIA GeForce RTX 5060", 4)]
    [InlineData("NVIDIA GeForce RTX 5070", 5)]
    [InlineData("NVIDIA GeForce RTX 5070 Laptop GPU", 5)]
    [InlineData("NVIDIA GeForce RTX 5080 Laptop GPU", 5)]
    public void Gpu_table_matches_the_approved_tiers(string gpu, int tier) =>
        Assert.Equal(tier, LaptopPerformanceTiers.GetGpuTier(gpu));

    [Fact]
    public void Matching_ignores_case()
    {
        Assert.Equal(4, LaptopPerformanceTiers.GetCpuTier("amd ryzen ai 7 pro 450"));
        Assert.Equal(5, LaptopPerformanceTiers.GetGpuTier("NVIDIA GEFORCE RTX 5070 LAPTOP GPU"));
    }

    [Theory]
    // "Intel Graphics" names no specific GPU, so it stays unknown by decision.
    [InlineData("Intel Graphics")]
    // Exact match only: no trimming, partial names or fuzzy matching.
    [InlineData("RTX 5070")]
    [InlineData("NVIDIA GeForce RTX 5070 ")]
    [InlineData("NVIDIA GeForce RTX 4060")]
    [InlineData("")]
    [InlineData(null)]
    public void Unlisted_gpu_names_are_unknown(string? gpu) =>
        Assert.Null(LaptopPerformanceTiers.GetGpuTier(gpu));

    [Theory]
    [InlineData("Ryzen AI 7 PRO 450")]
    [InlineData("Intel Core Ultra 7 155H vPro")]
    [InlineData("Apple M3")]
    [InlineData(null)]
    public void Unlisted_cpu_names_are_unknown(string? processor) =>
        Assert.Null(LaptopPerformanceTiers.GetCpuTier(processor));

    [Fact]
    public void Cpu_and_gpu_tables_are_separate()
    {
        Assert.Null(LaptopPerformanceTiers.GetCpuTier("AMD Radeon 860M"));
        Assert.Null(LaptopPerformanceTiers.GetGpuTier("AMD Ryzen 7 170"));
    }
}
