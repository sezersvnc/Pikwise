using Pikwise.Application.ExternalProducts.Models;
using Pikwise.Application.ExternalProducts.Normalization;

namespace Pikwise.DataImport;

public static class ReportPrinter
{
    public static void Print(IReadOnlyList<PreparedLaptop> laptops, LaptopImportReport report,
        IReadOnlyDictionary<string, DevelopmentPriceEntry> prices)
    {
        Console.WriteLine();
        Console.WriteLine($"=== Normalized laptops ({report.UsableCount} usable of {report.RequestedCount}) ===");
        foreach (var laptop in laptops.Where(item => item.IsUsable))
        {
            var r = laptop.Record!;
            Console.WriteLine($"[{r.ExternalId}] {LaptopNormalizer.BuildProductName(r.BrandName, r.ModelName)}");
            Console.WriteLine($"    CPU={Show(r.CpuName)} | GPU={Show(r.GpuName)} | RAM={Show(r.RamGb)} GB | Storage={Show(r.StorageGb)} GB");
            Console.WriteLine($"    Weight={Show(r.WeightKg)} kg | Refresh={Show(r.RefreshRateHz)} Hz | Screen={Show(r.ScreenSizeInch)}\" | Res={Show(r.Resolution)} | OS={Show(r.OperatingSystem)}");
            Console.WriteLine($"    Dev price entry: {(prices.ContainsKey(r.ExternalId) ? "present" : "missing")}");
            foreach (var issue in laptop.Issues) Console.WriteLine($"    note: {issue}");
        }

        if (report.Failures.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("=== Not usable ===");
            foreach (var failure in report.Failures) Console.WriteLine($"  {failure}");
        }

        Console.WriteLine();
        Console.WriteLine("=== Null count / ratio per field ===");
        foreach (var item in report.NullSummaries)
            Console.WriteLine($"  {item.Field,-16} {item.NullCount,3}/{item.Total,-3} ({item.NullRatio:P0})");

        Console.WriteLine();
        Console.WriteLine("=== Min / max (known values only) ===");
        foreach (var range in report.Ranges)
            Console.WriteLine($"  {range.Field,-16} min={Show(range.Min)} max={Show(range.Max)} known={range.KnownCount}");

        PrintNames("Unique CPU names", report.UniqueCpus);
        PrintNames("Unique GPU names", report.UniqueGpus);
        PrintNames("Brands", report.Brands);
    }

    private static void PrintNames(string title, IReadOnlyList<NameCount> names)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ({names.Count}) ===");
        foreach (var item in names) Console.WriteLine($"  {item.Count,3} x {item.Name}");
    }

    private static string Show<T>(T? value) => value is null ? "null" : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;
}
