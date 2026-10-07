using Pikwise.Domain.Entities;

namespace Pikwise.Application.ExternalProducts.Interfaces;

// Data access needed by the import use case. Infrastructure supplies the EF implementation.
public interface ILaptopImportRepository
{
    Task<bool> ReferenceExistsAsync(string provider, string externalId, CancellationToken cancellationToken = default);
    // Finds a brand by name or adds a new tracked one; nothing is saved until SaveChangesAsync.
    Task<Brand> GetOrAddBrandAsync(string name, CancellationToken cancellationToken = default);
    Task<Category> GetOrAddCategoryAsync(string name, CancellationToken cancellationToken = default);
    void Add(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
