using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pikwise.Domain.Entities;

namespace Pikwise.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(u => u.Id);
        // Preserve case-sensitive external identities even on a case-insensitive database.
        builder.Property(u => u.AuthProviderUserId).HasMaxLength(128).IsRequired()
            .UseCollation("Latin1_General_100_BIN2");
        // One external user identity maps to one local profile.
        builder.HasIndex(u => u.AuthProviderUserId).IsUnique();
        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(32).IsRequired();
    }
}
