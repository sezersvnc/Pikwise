using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pikwise.Application.ExternalProducts;

namespace Pikwise.Infrastructure.ExternalProducts.Icecat;

// Icecat JSON -> provider-neutral record. Feature names are matched case-insensitively against
// candidate lists; the first name in each list was confirmed on a live notebook, the rest are
// fallbacks. The DataImport `inspect` command prints the real names when a list needs adjusting.
public static partial class IcecatLaptopMapper
{
    // Verified against a live Icecat notebook (Session 11.5 inspect output).
    public static readonly string[] CpuModelNames = ["Processor model"];
    public static readonly string[] CpuManufacturerNames = ["Processor manufacturer"];
    public static readonly string[] CpuFamilyNames = ["Processor family"];
    // Discrete model first: when a laptop has both, the dedicated GPU is the relevant one.
    public static readonly string[] GpuNames = ["Discrete graphics card model", "On-board graphics card model", "Discrete graphics adapter model", "On-board graphics adapter model"];
    public static readonly string[] RamNames = ["Internal memory"];
    public static readonly string[] StorageNames = ["Total storage capacity", "Total SSDs capacity", "SSD capacity"];
    public static readonly string[] WeightNames = ["Weight"];
    public static readonly string[] RefreshNames = ["Maximum refresh rate", "Display refresh rate", "Refresh rate"];
    public static readonly string[] ScreenNames = ["Display diagonal"];
    public static readonly string[] ResolutionNames = ["Display resolution"];
    public static readonly string[] OsNames = ["Operating system installed"];

    public static ExternalLaptopRecord? Map(string externalId, JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return null;

        var features = ReadFeatures(data);
        var general = data.TryGetProperty("GeneralInfo", out var info) ? info : default;

        return new ExternalLaptopRecord(
            externalId,
            BrandName: ReadString(general, "Brand"),
            // ProductName is the marketing model; BrandPartCode is a SKU and only a last resort.
            ModelName: ReadString(general, "ProductName") ?? ReadString(general, "BrandPartCode"),
            CpuName: BuildCpuName(First(features, CpuManufacturerNames), First(features, CpuFamilyNames), First(features, CpuModelNames)),
            // "UMA" (unified memory architecture) describes memory sharing, not a GPU model.
            GpuName: First(features, GpuNames) is { } gpu && !gpu.Equals("UMA", StringComparison.OrdinalIgnoreCase) ? gpu : null,
            RamGb: ParseGigabytes(First(features, RamNames)),
            StorageGb: ParseGigabytes(First(features, StorageNames)),
            WeightKg: ParseWeightKg(First(features, WeightNames)),
            RefreshRateHz: ParseInt(First(features, RefreshNames)),
            ScreenSizeInch: ParseScreenInch(First(features, ScreenNames)),
            Resolution: First(features, ResolutionNames),
            OperatingSystem: First(features, OsNames));
    }

    // Every feature of the product keyed by its English name. Presentation text is preferred
    // because it carries the unit (for example "16 GB"); the raw value is the fallback.
    public static IReadOnlyDictionary<string, string> ReadFeatures(JsonElement data)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!data.TryGetProperty("FeaturesGroups", out var groups) || groups.ValueKind != JsonValueKind.Array)
            return result;
        foreach (var group in groups.EnumerateArray())
        {
            if (!group.TryGetProperty("Features", out var features) || features.ValueKind != JsonValueKind.Array) continue;
            foreach (var feature in features.EnumerateArray())
            {
                var name = FeatureName(feature);
                var value = ReadString(feature, "PresentationValue") ?? ReadString(feature, "RawValue");
                if (name is not null && value is not null) result.TryAdd(name, value);
            }
        }
        return result;
    }

    // Icecat's "Processor model" is often only the suffix ("450", "155H", "i9-13900H"), which is
    // ambiguous for the CPU tier table. The supplied family and manufacturer are prefixed:
    //   "AMD Ryzen AI 5" + "PRO 450"     -> "AMD Ryzen AI 5 PRO 450"
    //   "Intel Core Ultra 7" + "155H"    -> "Intel Core Ultra 7 155H"
    //   "Intel Core i9" + "i9-13900H"    -> "Intel Core i9-13900H" (shared "i9" not repeated)
    // All parts are provider facts; nothing is inferred.
    public static string? BuildCpuName(string? manufacturer, string? family, string? model)
    {
        if (model is null) return null;
        string name;
        if (family is null || model.StartsWith(family, StringComparison.OrdinalIgnoreCase))
            name = model;
        else
        {
            var familyWords = family.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var last = familyWords[^1];
            // Compare whole tokens: "i9-13900H" starts with token "i9", but "X1-26-100" does not start with "X".
            var firstModelToken = model.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            var prefix = string.Equals(firstModelToken, last, StringComparison.OrdinalIgnoreCase)
                ? string.Join(' ', familyWords[..^1])
                : family;
            name = prefix.Length == 0 ? model : $"{prefix} {model}";
        }
        if (manufacturer is null || name.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase)) return name;
        return $"{manufacturer} {name}";
    }

    // GeneralInfo fields used for brand and model, exposed for the inspect command.
    public static IReadOnlyDictionary<string, string> ReadGeneralInfo(JsonElement data)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!data.TryGetProperty("GeneralInfo", out var general)) return result;
        foreach (var name in new[] { "Brand", "ProductName", "Title", "BrandPartCode" })
            if (ReadString(general, name) is { } value) result[$"GeneralInfo.{name}"] = value;
        return result;
    }

    private static string? FeatureName(JsonElement feature)
    {
        if (feature.TryGetProperty("Feature", out var inner) && inner.TryGetProperty("Name", out var nameElement))
        {
            if (nameElement.ValueKind == JsonValueKind.String) return nameElement.GetString();
            if (nameElement.ValueKind == JsonValueKind.Object) return ReadString(nameElement, "Value");
        }
        return ReadString(feature, "Name");
    }

    private static string? ReadString(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value)) return null;
        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.Object => ReadString(value, "Value") ?? ReadString(value, "Name"),
            _ => null
        };
        return LaptopNormalizer.CleanText(text);
    }

    // Icecat sometimes stores "no value" as text; these are treated as missing, never as a value.
    private static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
        { "Not available", "N/A", "NA", "No", "None", "-", "Not specified" };

    private static string? First(IReadOnlyDictionary<string, string> features, string[] candidates)
    {
        foreach (var candidate in candidates)
            if (features.TryGetValue(candidate, out var value) && !Placeholders.Contains(value)) return value;
        return null;
    }

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    // "16 GB" -> 16, "1 TB" -> 1000, "512GB" -> 512. Megabyte and other units are not guessed.
    public static int? ParseGigabytes(string? text)
    {
        var match = text is null ? null : QuantityRegex().Match(text);
        if (match is null || !match.Success) return null;
        if (!decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, Invariant, out var number)) return null;
        var unit = match.Groups[2].Value.ToUpperInvariant();
        decimal gigabytes = unit switch { "GB" => number, "TB" => number * 1000m, _ => -1m };
        return gigabytes > 0 && gigabytes <= int.MaxValue ? (int)Math.Round(gigabytes) : null;
    }

    // Weight arrives as "1.8 kg", "1800 g" or "3.5 lbs"; everything is converted to kilograms.
    public static decimal? ParseWeightKg(string? text)
    {
        var match = text is null ? null : WeightRegex().Match(text);
        if (match is null || !match.Success) return null;
        if (!decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, Invariant, out var number)) return null;
        return match.Groups[2].Value.ToLowerInvariant() switch
        {
            "kg" => number,
            "g" => number / 1000m,
            "lb" or "lbs" => number * 0.45359237m,
            _ => null
        };
    }

    // Icecat reports the diagonal in centimeters and often inches in brackets, e.g. "39.6 cm (15.6\")".
    public static decimal? ParseScreenInch(string? text)
    {
        if (text is null) return null;
        var inch = InchRegex().Match(text);
        if (inch.Success && decimal.TryParse(inch.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, Invariant, out var inches))
            return inches;
        var cm = CentimeterRegex().Match(text);
        if (cm.Success && decimal.TryParse(cm.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, Invariant, out var centimeters))
            return decimal.Round(centimeters / 2.54m, 1);
        return null;
    }

    public static int? ParseInt(string? text)
    {
        var match = text is null ? null : IntegerRegex().Match(text);
        return match is { Success: true } && int.TryParse(match.Value, NumberStyles.None, Invariant, out var value) ? value : null;
    }

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(GB|TB|MB)\b", RegexOptions.IgnoreCase)]
    private static partial Regex QuantityRegex();

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(kg|g|lbs|lb)\b", RegexOptions.IgnoreCase)]
    private static partial Regex WeightRegex();

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(?:""|”|″|in\b|inch)", RegexOptions.IgnoreCase)]
    private static partial Regex InchRegex();

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*cm", RegexOptions.IgnoreCase)]
    private static partial Regex CentimeterRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex IntegerRegex();
}
