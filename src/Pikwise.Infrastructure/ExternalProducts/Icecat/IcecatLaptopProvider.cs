using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Pikwise.Application.ExternalProducts;

namespace Pikwise.Infrastructure.ExternalProducts.Icecat;

// First implementation of IExternalLaptopProvider, backed by the Open Icecat JSON API.
public sealed class IcecatLaptopProvider(HttpClient httpClient, IOptions<IcecatOptions> options) : IExternalLaptopProvider
{
    private readonly IcecatOptions settings = options.Value;
    private bool firstRequest = true;

    public string ProviderName => "icecat";

    public async Task<ExternalLaptopRecord?> GetLaptopAsync(string externalId, CancellationToken cancellationToken = default)
    {
        using var document = await FetchAsync(externalId, cancellationToken);
        return document is null ? null : IcecatLaptopMapper.Map(externalId, document);
    }

    // Raw feature names and values of one product, used to verify the mapper's candidate names.
    public async Task<IReadOnlyDictionary<string, string>?> GetFeaturesAsync(string externalId, CancellationToken cancellationToken = default)
    {
        using var document = await FetchAsync(externalId, cancellationToken);
        if (document is null || !document.RootElement.TryGetProperty("data", out var data)) return null;
        var result = new Dictionary<string, string>(IcecatLaptopMapper.ReadGeneralInfo(data), StringComparer.OrdinalIgnoreCase);
        foreach (var pair in IcecatLaptopMapper.ReadFeatures(data)) result.TryAdd(pair.Key, pair.Value);
        return result;
    }

    private async Task<JsonDocument?> FetchAsync(string externalId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Username))
            throw new InvalidOperationException("Configure Icecat:Username using User Secrets or environment variables.");
        if (!externalId.All(char.IsDigit))
            throw new ArgumentException("Icecat product identifiers are numeric.", nameof(externalId));

        // Fair-use rate limit: one request at a time with a pause between requests.
        if (!firstRequest && settings.RequestDelayMilliseconds > 0)
            await Task.Delay(settings.RequestDelayMilliseconds, cancellationToken);
        firstRequest = false;

        var url = $"{settings.LiveBaseUrl}?lang={Uri.EscapeDataString(settings.Language)}" +
                  $"&username={Uri.EscapeDataString(settings.Username)}&icecat_id={externalId}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        // The URL contains the username, so it is deliberately left out of the error text.
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Icecat returned HTTP {(int)response.StatusCode} for product {externalId}.");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        // Icecat reports unavailable products with an error message in the body.
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            return null;
        }
        return document;
    }
}
