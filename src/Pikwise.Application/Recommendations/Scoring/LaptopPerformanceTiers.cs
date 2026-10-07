namespace Pikwise.Application.Recommendations.Scoring;

// Hand-maintained CPU/GPU tier table (RECOMMENDATION_ENGINE.md decision 15, ADR-021).
// Names match the importer's normalized names exactly, ignoring case; there is no fuzzy matching.
// An unlisted name is unknown. "Intel Graphics" is deliberately absent: it names no specific GPU.
public static class LaptopPerformanceTiers
{
    public const int MinTier = 1;
    public const int MaxTier = 5;

    private static readonly Dictionary<string, int> CpuTiers = Build(
        (1, ["Intel Core i3-1305U"]),
        (2, ["Intel Core i7-1255U", "Intel Core i7-1265U", "Intel Core 5 120U", "Qualcomm Snapdragon X1-26-100",
            "Intel Core Ultra 5 325"]),
        (3, ["AMD Ryzen 7 170", "AMD Ryzen 5 7533HS", "Intel Core Ultra 7 155U", "Intel Core Ultra 7 355",
            "AMD Ryzen AI 5 PRO 435"]),
        (4, ["Intel Core Ultra 7 155H", "Intel Core Ultra 7 366H", "AMD Ryzen AI 7 350", "AMD Ryzen AI 7 PRO 450",
            "AMD Ryzen 7 260"]),
        (5, ["Intel Core Ultra 9 386H", "Intel Core 9 270H", "Intel Core Ultra 7 255HX"]));

    private static readonly Dictionary<string, int> GpuTiers = Build(
        (1, ["Intel UHD Graphics"]),
        (2, ["Intel Iris Xe Graphics", "AMD Radeon 680M", "AMD Radeon 840M"]),
        (3, ["AMD Radeon 860M", "Intel Arc Graphics"]),
        (4, ["NVIDIA GeForce RTX 4050", "NVIDIA GeForce RTX 5050", "NVIDIA GeForce RTX 5060"]),
        (5, ["NVIDIA GeForce RTX 5070", "NVIDIA GeForce RTX 5070 Laptop GPU", "NVIDIA GeForce RTX 5080 Laptop GPU"]));

    public static int? GetCpuTier(string? processor) => Lookup(CpuTiers, processor);

    public static int? GetGpuTier(string? gpu) => Lookup(GpuTiers, gpu);

    private static int? Lookup(Dictionary<string, int> tiers, string? name) =>
        name is not null && tiers.TryGetValue(name, out var tier) ? tier : null;

    private static Dictionary<string, int> Build(params (int Tier, string[] Names)[] rows) =>
        rows.SelectMany(row => row.Names.Select(name => (name, row.Tier)))
            .ToDictionary(entry => entry.name, entry => entry.Tier, StringComparer.OrdinalIgnoreCase);
}
