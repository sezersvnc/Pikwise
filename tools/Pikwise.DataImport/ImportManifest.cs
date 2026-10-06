using System.Text.Json;

namespace Pikwise.DataImport;

public sealed record ManifestEntry(string ExternalId, string SupplierId, string? BrandPartCode, string? ModelName);

// Small reusable list of provider product IDs, so later runs never re-scan the Icecat index.
public sealed record ImportManifest(string Provider, DateTimeOffset CreatedAt, IReadOnlyList<ManifestEntry> Entries)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static ImportManifest Load(string path) =>
        JsonSerializer.Deserialize<ImportManifest>(File.ReadAllText(path))
        ?? throw new InvalidOperationException($"Manifest {path} is empty.");

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
}
