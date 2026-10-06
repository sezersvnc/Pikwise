using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Options;

namespace Pikwise.Infrastructure.ExternalProducts.Icecat;

public sealed record IcecatCategoryMatch(string CategoryId, string? LangId, string Name);

public sealed record IcecatCandidate(string ExternalId, string SupplierId, string? BrandPartCode, string? ModelName, string? Quality);

// Streams the Open Icecat index file only to pick a small set of laptop product IDs.
// The catalog is never imported: nothing is kept except the selected candidates.
public sealed class IcecatIndexDiscovery(HttpClient httpClient, IOptions<IcecatOptions> options)
{
    private readonly IcecatOptions settings = options.Value;

    // Selection rules (both deliberate, so the sample resembles a current catalog):
    //   - brand variety from the most represented laptop suppliers, which skips test and
    //     misfiled suppliers that have only one or two entries in the laptop category;
    //   - newest products per supplier, using the highest Product_ID as the recency signal
    //     (Icecat assigns IDs sequentially), so 2000s-era laptops are not picked.
    public async Task<IReadOnlyList<IcecatCandidate>> DiscoverAsync(
        int count, int supplierCount, int maxEntriesToScan, IReadOnlyCollection<string>? categoryIds,
        IReadOnlyCollection<string>? excludedSuppliers, IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        // An explicit category id (found with the `categories` command) skips the name lookup.
        var laptopCategoryIds = categoryIds is { Count: > 0 }
            ? categoryIds.ToHashSet()
            : (await SearchCategoriesAsync("Notebooks", exact: true, cancellationToken))
                .Where(match => match.LangId is null or "1").Select(match => match.CategoryId).ToHashSet();
        if (laptopCategoryIds.Count == 0)
            throw new InvalidOperationException(
                "No category named 'Notebooks' was found. Run `categories --search notebook` and pass --category-id.");
        progress?.Report($"Laptop category id(s): {string.Join(", ", laptopCategoryIds)}");

        // Per supplier: number of laptop entries and the newest few candidates. Only these small
        // summaries are kept in memory while the index streams past.
        var perSupplierKeep = Math.Max(1, (int)Math.Ceiling((double)count / Math.Max(1, supplierCount)) + 2);
        var laptopCounts = new Dictionary<string, int>();
        var newest = new Dictionary<string, List<(long Id, IcecatCandidate Candidate)>>();
        var scanned = 0;
        await using var stream = await OpenGzipAsync(settings.IndexUrl, cancellationToken);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, Async = true });
        while (await reader.ReadAsync())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Name != "file") continue;
            if (++scanned > maxEntriesToScan) break;
            if (scanned % 1_000_000 == 0) progress?.Report($"Scanned {scanned} index entries...");
            var catId = reader.GetAttribute("Catid");
            var supplier = reader.GetAttribute("Supplier_id");
            var productId = reader.GetAttribute("Product_ID");
            if (catId is null || supplier is null || productId is null || !laptopCategoryIds.Contains(catId)) continue;
            if (!long.TryParse(productId, out var numericId)) continue;
            // Suppliers excluded by the operator, e.g. refurbishers or brands without open data.
            if (excludedSuppliers?.Contains(supplier) == true) continue;
            // Only richly described entries are useful for specification data.
            var quality = reader.GetAttribute("Quality");
            if (!string.Equals(quality, "ICECAT", StringComparison.OrdinalIgnoreCase)) continue;

            laptopCounts[supplier] = laptopCounts.GetValueOrDefault(supplier) + 1;
            if (!newest.TryGetValue(supplier, out var list)) newest[supplier] = list = [];
            // One entry per model name per supplier (newest wins), so SKUs of the same model
            // with identical names do not fill the sample.
            var modelName = reader.GetAttribute("Model_Name");
            var sameModel = modelName is null ? -1 : list.FindIndex(item =>
                string.Equals(item.Candidate.ModelName, modelName, StringComparison.OrdinalIgnoreCase));
            if (sameModel >= 0)
            {
                if (numericId > list[sameModel].Id)
                    list[sameModel] = (numericId, new IcecatCandidate(productId, supplier,
                        reader.GetAttribute("Prod_id"), modelName, quality));
                list.Sort((a, b) => b.Id.CompareTo(a.Id));
                continue;
            }
            if (list.Count < perSupplierKeep || numericId > list[^1].Id)
            {
                list.Add((numericId, new IcecatCandidate(productId, supplier,
                    reader.GetAttribute("Prod_id"), reader.GetAttribute("Model_Name"), quality)));
                list.Sort((a, b) => b.Id.CompareTo(a.Id));
                if (list.Count > perSupplierKeep) list.RemoveAt(list.Count - 1);
            }
        }
        progress?.Report($"Scanned {scanned} index entries, {laptopCounts.Count} supplier(s) with laptops.");

        var topSuppliers = laptopCounts.OrderByDescending(item => item.Value).ThenBy(item => item.Key, StringComparer.Ordinal)
            .Take(supplierCount).Select(item => item.Key).ToList();
        foreach (var supplier in topSuppliers)
            progress?.Report($"  supplier {supplier}: {laptopCounts[supplier]} laptop entries");

        // Round-robin across the chosen suppliers, newest first, for brand variety.
        var picked = new List<IcecatCandidate>();
        for (var round = 0; picked.Count < count; round++)
        {
            var added = false;
            foreach (var supplier in topSuppliers)
            {
                var list = newest[supplier];
                if (round >= list.Count || picked.Count >= count) continue;
                picked.Add(list[round].Candidate);
                added = true;
            }
            if (!added) break;
        }
        return picked;
    }

    // Category names matching the term. Only names directly under <Category> count, so names
    // inside nested elements such as <ParentCategory> are not attributed to the wrong category.
    // The name is read from a Value attribute or, if absent, from the element text.
    public async Task<IReadOnlyList<IcecatCategoryMatch>> SearchCategoriesAsync(
        string term, bool exact, CancellationToken cancellationToken = default)
    {
        var matches = new List<IcecatCategoryMatch>();
        await using var stream = await OpenGzipAsync(settings.CategoriesUrl, cancellationToken);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, Async = true });
        string? currentId = null;
        var categoryDepth = -1;
        string? pendingLangId = null;
        var pendingText = false;
        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Name given as element text: <Name langid="1">Notebooks</Name>.
            if (pendingText && reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
            {
                AddIfMatch(reader.Value, pendingLangId);
                pendingText = false;
                continue;
            }
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Name") pendingText = false;
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Category" && reader.Depth == categoryDepth)
            {
                currentId = null;
                categoryDepth = -1;
                continue;
            }
            if (reader.NodeType != XmlNodeType.Element) continue;
            if (reader.Name == "Category" && categoryDepth < 0)
            {
                currentId = reader.GetAttribute("ID");
                categoryDepth = reader.IsEmptyElement ? -1 : reader.Depth;
                continue;
            }
            // Accept <Name> as a direct child of <Category> or inside its <Names> wrapper.
            if (reader.Name != "Name" || currentId is null || reader.Depth > categoryDepth + 2) continue;
            var langId = reader.GetAttribute("langid");
            var value = reader.GetAttribute("Value");
            if (value is not null) AddIfMatch(value, langId);
            else if (!reader.IsEmptyElement) { pendingText = true; pendingLangId = langId; }
        }
        return matches;

        void AddIfMatch(string? value, string? langId)
        {
            if (string.IsNullOrWhiteSpace(value) || currentId is null) return;
            var isMatch = exact
                ? string.Equals(value.Trim(), term, StringComparison.OrdinalIgnoreCase)
                : value.Contains(term, StringComparison.OrdinalIgnoreCase);
            if (isMatch) matches.Add(new IcecatCategoryMatch(currentId, langId, value.Trim()));
        }
    }

    // First characters of the decompressed category file, to see its real XML structure.
    public async Task<string> ReadCategoriesHeadAsync(int characters, CancellationToken cancellationToken = default)
    {
        await using var stream = await OpenGzipAsync(settings.CategoriesUrl, cancellationToken);
        using var reader = new StreamReader(stream);
        var buffer = new char[characters];
        var read = await reader.ReadBlockAsync(buffer.AsMemory(), cancellationToken);
        return new string(buffer, 0, read);
    }

    private async Task<Stream> OpenGzipAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password))
            throw new InvalidOperationException("Configure Icecat:Username and Icecat:Password using User Secrets or environment variables.");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        // ResponseHeadersRead streams the large file instead of buffering it in memory.
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            throw new HttpRequestException($"Icecat returned HTTP {(int)response.StatusCode} for {new Uri(url).AbsolutePath}.");
        }
        var raw = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new GZipStream(raw, CompressionMode.Decompress);
    }
}
