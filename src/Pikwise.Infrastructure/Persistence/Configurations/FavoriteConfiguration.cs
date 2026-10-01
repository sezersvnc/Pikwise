using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pikwise.Domain.Entities;

namespace Pikwise.Infrastructure.Persistence.Configurations;

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites");
        builder.HasKey(f => new { f.UserProfileId, f.ProductId });
        builder.HasOne(f => f.UserProfile).WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.Product).WithMany(p => p.Favorites)
            .HasForeignKey(f => f.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
