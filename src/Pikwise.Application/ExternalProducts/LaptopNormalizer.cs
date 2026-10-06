using System.Text.RegularExpressions;

namespace Pikwise.Application.ExternalProducts;

// Result of normalization: the cleaned record and notes about values that were dropped.
public sealed record NormalizedLaptop(ExternalLaptopRecord Record, IReadOnlyList<string> Issues);

// Provider-independent cleanup that makes external values fit the Pikwise specification model.
// It trims and standardizes formats but never invents or estimates a missing value.
public static partial class LaptopNormalizer
{
    // These limits mirror the SQL column lengths of Product and LaptopSpecification.
    public const int MaxNameLength = 200;
    public const int MaxProcessorLength = 200;
    public const int MaxGpuLength = 200;
    public const int MaxResolutionLength = 50;
    public const int MaxOperatingSystemLength = 100;

    public static NormalizedLaptop Normalize(ExternalLaptopRecord record)
    {
        var issues = new List<string>();
        var normalized = record with
        {
            ExternalId = record.ExternalId.Trim(),
            BrandName = CleanText(record.BrandName),
            ModelName = CleanText(record.ModelName),
            CpuName = Limit(CleanText(record.CpuName), MaxProcessorLength, "CpuName", issues),
            GpuName = Limit(CleanText(record.GpuName), MaxGpuLength, "GpuName", issues),
            RamGb = Positive(record.RamGb, "RamGb", issues),
            StorageGb = Positive(record.StorageGb, "StorageGb", issues),
            RefreshRateHz = Positive(record.RefreshRateHz, "RefreshRateHz", issues),
            // Round to the SQL precision so storing the value never silently changes it again.
            WeightKg = Positive(record.WeightKg, "WeightKg", issues) is { } weight ? decimal.Round(weight, 3) : null,
            ScreenSizeInch = Positive(record.ScreenSizeInch, "ScreenSizeInch", issues) is { } screen ? decimal.Round(screen, 2) : null,
            Resolution = Limit(NormalizeResolution(record.Resolution), MaxResolutionLength, "Resolution", issues),
            OperatingSystem = Limit(CleanText(record.OperatingSystem), MaxOperatingSystemLength, "OperatingSystem", issues)
        };
        return new NormalizedLaptop(normalized, issues);
    }

    // The product name is "Brand Model", without repeating the brand when the model already starts with it.
    public static string? BuildProductName(string? brandName, string? modelName)
    {
        if (modelName is null) return null;
        if (brandName is null) return modelName;
        return modelName.StartsWith(brandName, StringComparison.OrdinalIgnoreCase)
            ? modelName
            : $"{brandName} {modelName}";
    }

    // Trim, collapse repeated whitespace and turn blank text into null.
    public static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        // Trademark symbols are formatting, not data ("Intel® Core™ i9" -> "Intel Core i9").
        var text = value.Replace("®", string.Empty).Replace("™", string.Empty);
        if (string.IsNullOrWhiteSpace(text)) return null;
        return WhitespaceRegex().Replace(text.Trim(), " ");
    }

    // "1920 x 1080 pixels" and "1920×1080" become "1920x1080". Unrecognized text is kept as supplied.
    private static string? NormalizeResolution(string? value)
    {
        var text = CleanText(value);
        if (text is null) return null;
        var match = ResolutionRegex().Match(text);
        return match.Success ? $"{match.Groups[1].Value}x{match.Groups[2].Value}" : text;
    }

    private static string? Limit(string? value, int maxLength, string field, List<string> issues)
    {
        if (value is null || value.Length <= maxLength) return value;
        // Truncating would change the fact, so the value is dropped and reported instead.
        issues.Add($"{field} exceeds {maxLength} characters and was dropped.");
        return null;
    }

    private static int? Positive(int? value, string field, List<string> issues)
    {
        if (value is null or > 0) return value;
        issues.Add($"{field} was not positive and was dropped.");
        return null;
    }

    private static decimal? Positive(decimal? value, string field, List<string> issues)
    {
        if (value is null or > 0) return value;
        issues.Add($"{field} was not positive and was dropped.");
        return null;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(\d{3,5})\s*[xX×]\s*(\d{3,5})")]
    private static partial Regex ResolutionRegex();
}
