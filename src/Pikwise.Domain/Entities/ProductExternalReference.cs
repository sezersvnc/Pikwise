namespace Pikwise.Domain.Entities;

// Links a product to the record it was imported from, without provider-specific fields on Product.
public sealed class ProductExternalReference
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    // Free-text provider key such as "icecat"; Domain does not know any provider.
    public required string Provider { get; set; }
    public required string ExternalId { get; set; }
    public DateTimeOffset ImportedAt { get; set; }
    public Product Product { get; set; } = null!;
}
