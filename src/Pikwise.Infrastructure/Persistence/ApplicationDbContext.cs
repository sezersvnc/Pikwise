using Microsoft.EntityFrameworkCore;
using Pikwise.Domain.Entities;

namespace Pikwise.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    // Text columns state their collation so matching and ordering do not depend on the server's
    // default (a Turkish server makes "intel" differ from "Intel"). ADR-030.
    public const string DefaultTextCollation = "SQL_Latin1_General_CP1_CI_AS";

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<LaptopSpecification> LaptopSpecifications => Set<LaptopSpecification>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ProductExternalReference> ProductExternalReferences => Set<ProductExternalReference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Discover Fluent API mappings here so Domain entities stay independent of EF Core.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        // Columns with an explicit collation (binary identifiers) keep it.
        var textProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.ClrType == typeof(string) && property.GetCollation() is null);
        foreach (var property in textProperties) property.SetCollation(DefaultTextCollation);
    }
}
