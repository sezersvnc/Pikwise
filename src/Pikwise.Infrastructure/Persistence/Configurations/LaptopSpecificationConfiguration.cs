using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pikwise.Domain.Entities;

namespace Pikwise.Infrastructure.Persistence.Configurations;

public sealed class LaptopSpecificationConfiguration : IEntityTypeConfiguration<LaptopSpecification>
{
    public void Configure(EntityTypeBuilder<LaptopSpecification> builder)
    {
        builder.ToTable("LaptopSpecifications");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Processor).HasMaxLength(200).IsRequired();
        builder.Property(s => s.GPU).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Resolution).HasMaxLength(50).IsRequired();
        builder.Property(s => s.OperatingSystem).HasMaxLength(100).IsRequired();
        builder.Property(s => s.ScreenSize).HasPrecision(5, 2);
        builder.Property(s => s.Weight).HasPrecision(6, 3);
        builder.HasOne(s => s.Product).WithOne(p => p.LaptopSpecification)
            .HasForeignKey<LaptopSpecification>(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => s.ProductId).IsUnique();
    }
}
