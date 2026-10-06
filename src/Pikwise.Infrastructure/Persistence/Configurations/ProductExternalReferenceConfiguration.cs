using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pikwise.Domain.Entities;

namespace Pikwise.Infrastructure.Persistence.Configurations;

public sealed class ProductExternalReferenceConfiguration : IEntityTypeConfiguration<ProductExternalReference>
{
    public void Configure(EntityTypeBuilder<ProductExternalReference> builder)
    {
        builder.ToTable("ProductExternalReferences");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Provider).HasMaxLength(50).IsRequired();
        // Provider identifiers are opaque and compared exactly, so use a binary collation.
        builder.Property(r => r.ExternalId).HasMaxLength(100).IsRequired().UseCollation("Latin1_General_100_BIN2");
        // Deleting a product removes its import references.
        builder.HasOne(r => r.Product).WithMany(p => p.ExternalReferences)
            .HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
        // The same external record can be imported at most once per provider.
        builder.HasIndex(r => new { r.Provider, r.ExternalId }).IsUnique();
        builder.HasIndex(r => r.ProductId);
    }
}
