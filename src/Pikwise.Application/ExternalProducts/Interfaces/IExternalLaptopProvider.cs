using Pikwise.Application.ExternalProducts.Models;

namespace Pikwise.Application.ExternalProducts.Interfaces;

// Application-side contract for an external laptop data source. Implementations live in
// Infrastructure so no provider schema, URL or credential reaches Application or Domain.
public interface IExternalLaptopProvider
{
    // Stable lowercase key stored with imported products, for example "icecat".
    string ProviderName { get; }

    // Returns null when the provider has no record for the identifier.
    Task<ExternalLaptopRecord?> GetLaptopAsync(string externalId, CancellationToken cancellationToken = default);
}
