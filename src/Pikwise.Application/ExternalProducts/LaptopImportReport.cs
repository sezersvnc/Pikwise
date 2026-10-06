namespace Pikwise.Application.ExternalProducts;

public sealed record FieldNullSummary(string Field, int NullCount, int Total)
{
    public double NullRatio => Total == 0 ? 0 : (double)NullCount / Total;
}

public sealed record NumericRange(string Field, decimal? Min, decimal? Max, int KnownCount);

public sealed record NameCount(string Name, int Count);

// Summary of normalized laptops used to review real data before any tier table or reference range is chosen.
public sealed record LaptopImportReport(
    int RequestedCount,
    int UsableCount,
    IReadOnlyList<string> Failures,
    IReadOnlyList<FieldNullSummary> NullSummaries,
    IReadOnlyList<NumericRange> Ranges,
    IReadOnlyList<NameCount> UniqueCpus,
    IReadOnlyList<NameCount> UniqueGpus,
    IReadOnlyList<NameCount> Brands);

public static class LaptopImportReportBuilder
{
    public static LaptopImportReport Build(IReadOnlyList<PreparedLaptop> laptops)
    {
        var usable = laptops.Where(laptop => laptop.IsUsable).Select(laptop => laptop.Record!).ToList();
        var failures = laptops.Where(laptop => !laptop.IsUsable)
            .Select(laptop => $"{laptop.ExternalId}: {laptop.FailureReason}").ToList();

        var nullSummaries = new List<FieldNullSummary>
        {
            Null("CpuName", usable, r => r.CpuName is null),
            Null("GpuName", usable, r => r.GpuName is null),
            Null("RamGb", usable, r => r.RamGb is null),
            Null("StorageGb", usable, r => r.StorageGb is null),
            Null("WeightKg", usable, r => r.WeightKg is null),
            Null("RefreshRateHz", usable, r => r.RefreshRateHz is null),
            Null("ScreenSizeInch", usable, r => r.ScreenSizeInch is null),
            Null("Resolution", usable, r => r.Resolution is null),
            Null("OperatingSystem", usable, r => r.OperatingSystem is null)
        };
        var ranges = new List<NumericRange>
        {
            Range("RamGb", usable.Select(r => (decimal?)r.RamGb)),
            Range("StorageGb", usable.Select(r => (decimal?)r.StorageGb)),
            Range("WeightKg", usable.Select(r => r.WeightKg)),
            Range("RefreshRateHz", usable.Select(r => (decimal?)r.RefreshRateHz)),
            Range("ScreenSizeInch", usable.Select(r => r.ScreenSizeInch))
        };
        return new LaptopImportReport(laptops.Count, usable.Count, failures, nullSummaries, ranges,
            Count(usable.Select(r => r.CpuName)), Count(usable.Select(r => r.GpuName)),
            Count(usable.Select(r => r.BrandName)));
    }

    private static FieldNullSummary Null(string field, List<ExternalLaptopRecord> records, Func<ExternalLaptopRecord, bool> isNull) =>
        new(field, records.Count(isNull), records.Count);

    private static NumericRange Range(string field, IEnumerable<decimal?> values)
    {
        var known = values.Where(value => value is not null).Select(value => value!.Value).ToList();
        return known.Count == 0 ? new NumericRange(field, null, null, 0)
            : new NumericRange(field, known.Min(), known.Max(), known.Count);
    }

    // Unique names with counts, most frequent first and then alphabetical so output is stable.
    private static IReadOnlyList<NameCount> Count(IEnumerable<string?> names) =>
        names.Where(name => name is not null).GroupBy(name => name!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new NameCount(group.First()!, group.Count()))
            .OrderByDescending(item => item.Count).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
