namespace Pikwise.Application.ExternalProducts.Models;

// Provider-neutral laptop data. Providers parse their own formats into these typed, nullable
// values; null always means "not supplied", never a guess.
public sealed record ExternalLaptopRecord(
    string ExternalId,
    string? BrandName,
    string? ModelName,
    string? CpuName,
    string? GpuName,
    int? RamGb,
    int? StorageGb,
    decimal? WeightKg,
    int? RefreshRateHz,
    decimal? ScreenSizeInch,
    string? Resolution,
    string? OperatingSystem);
