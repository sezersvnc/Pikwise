using Pikwise.Application.ExternalProducts.Interfaces;
using Pikwise.Application.ExternalProducts.Models;
using Pikwise.Application.ExternalProducts.Normalization;

namespace Pikwise.Application.ExternalProducts.Services;

// Fetches records from a provider and normalizes them. It never touches the database,
// so the same step powers the dry run and the real import.
public sealed class LaptopImportPreparer(IExternalLaptopProvider provider)
{
    public async Task<IReadOnlyList<PreparedLaptop>> PrepareAsync(
        IReadOnlyList<string> externalIds, IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PreparedLaptop>(externalIds.Count);
        foreach (var rawId in externalIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var externalId = rawId.Trim();
            progress?.Report($"Fetching {externalId} from {provider.ProviderName}");
            results.Add(await PrepareOneAsync(externalId, cancellationToken));
        }
        return results;
    }

    private async Task<PreparedLaptop> PrepareOneAsync(string externalId, CancellationToken cancellationToken)
    {
        ExternalLaptopRecord? record;
        try
        {
            record = await provider.GetLaptopAsync(externalId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // One failing product must not abort the whole run; the reason is reported instead.
            return new PreparedLaptop(externalId, null, [], $"Fetch failed: {exception.GetType().Name}: {exception.Message}");
        }
        if (record is null) return new PreparedLaptop(externalId, null, [], "Provider has no record for this identifier.");

        var normalized = LaptopNormalizer.Normalize(record with { ExternalId = externalId });
        // A product cannot be named or grouped without a brand and a model.
        if (normalized.Record.BrandName is null || normalized.Record.ModelName is null)
            return new PreparedLaptop(externalId, normalized.Record, normalized.Issues, "Brand or model is missing.");
        // Entries filed under laptops that carry no laptop hardware data (test records, accessories)
        // are of no use to the recommendation engine.
        var r = normalized.Record;
        if (r.CpuName is null && r.GpuName is null && r.RamGb is null && r.StorageGb is null)
            return new PreparedLaptop(externalId, r, normalized.Issues, "No CPU, GPU, RAM or storage data supplied.");
        var name = LaptopNormalizer.BuildProductName(normalized.Record.BrandName, normalized.Record.ModelName);
        if (name is null || name.Length > LaptopNormalizer.MaxNameLength)
            return new PreparedLaptop(externalId, normalized.Record, normalized.Issues, "Product name is empty or too long.");
        return new PreparedLaptop(externalId, normalized.Record, normalized.Issues, null);
    }
}
