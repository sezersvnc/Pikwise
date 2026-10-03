using Microsoft.EntityFrameworkCore;
using Pikwise.Domain.Entities;

namespace Pikwise.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<LaptopSpecification> LaptopSpecifications => Set<LaptopSpecification>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Discover Fluent API mappings here so Domain entities stay independent of EF Core.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
