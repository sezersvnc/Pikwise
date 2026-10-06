using Pikwise.Domain.Entities;

namespace Pikwise.Application.ExternalProducts;

public enum ImportStatus { Imported, AlreadyImported, SkippedNoDevelopmentPrice, SkippedUnusable }

public sealed record ImportItemResult(string ExternalId, string? ProductName, ImportStatus Status, string? Reason);

public sealed record ImportOutcome(IReadOnlyList<ImportItemResult> Items)
{
    public int Count(ImportStatus status) => Items.Count(item => item.Status == status);
}

// Writes prepared laptops to the database. Price, stock and active flag come only from the
// companion development CSV; a laptop without an entry is skipped rather than given invented values.
public sealed class LaptopImportService(ILaptopImportRepository repository, TimeProvider? timeProvider = null)
{
    public const string LaptopCategoryName = "Laptop";
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task<ImportOutcome> ImportAsync(
        string providerName, IReadOnlyList<PreparedLaptop> laptops,
        IReadOnlyDictionary<string, DevelopmentPriceEntry> developmentPrices,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ImportItemResult>();
        foreach (var laptop in laptops)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ImportOneAsync(providerName, laptop, developmentPrices, cancellationToken));
        }
        return new ImportOutcome(results);
    }

    private async Task<ImportItemResult> ImportOneAsync(
        string providerName, PreparedLaptop laptop,
        IReadOnlyDictionary<string, DevelopmentPriceEntry> developmentPrices, CancellationToken cancellationToken)
    {
        if (!laptop.IsUsable || laptop.Record is null)
            return new ImportItemResult(laptop.ExternalId, null, ImportStatus.SkippedUnusable, laptop.FailureReason);
        var record = laptop.Record;
        var name = LaptopNormalizer.BuildProductName(record.BrandName, record.ModelName)!;

        // Duplicate imports are prevented by the unique (Provider, ExternalId) reference.
        if (await repository.ReferenceExistsAsync(providerName, laptop.ExternalId, cancellationToken))
            return new ImportItemResult(laptop.ExternalId, name, ImportStatus.AlreadyImported, null);
        if (!developmentPrices.TryGetValue(laptop.ExternalId, out var price))
            return new ImportItemResult(laptop.ExternalId, name, ImportStatus.SkippedNoDevelopmentPrice,
                "No row for this ExternalId in the development price CSV.");

        var now = clock.GetUtcNow();
        var brand = await repository.GetOrAddBrandAsync(record.BrandName!, cancellationToken);
        var category = await repository.GetOrAddCategoryAsync(LaptopCategoryName, cancellationToken);
        var product = new Product
        {
            Name = name, Price = price.Price, Stock = price.Stock, IsActive = price.IsActive,
            Brand = brand, Category = category, CreatedAt = now,
            LaptopSpecification = new LaptopSpecification
            {
                Processor = record.CpuName, GPU = record.GpuName, RamGb = record.RamGb,
                StorageGb = record.StorageGb, ScreenSize = record.ScreenSizeInch,
                Resolution = record.Resolution, RefreshRate = record.RefreshRateHz,
                Weight = record.WeightKg, OperatingSystem = record.OperatingSystem
            }
        };
        product.ExternalReferences.Add(new ProductExternalReference
        {
            Provider = providerName, ExternalId = laptop.ExternalId, ImportedAt = now
        });
        repository.Add(product);
        // Save per product so one failure leaves earlier products imported and the lookup rows visible.
        await repository.SaveChangesAsync(cancellationToken);
        return new ImportItemResult(laptop.ExternalId, name, ImportStatus.Imported, null);
    }
}
