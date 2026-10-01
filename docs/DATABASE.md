# DATABASE.md — Pikwise V0.1

## Database
SQL Server via Entity Framework Core.

Recommended EF Core provider:

```text
Microsoft.EntityFrameworkCore.SqlServer
```

Typical registration:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
```

Example development connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PikwiseDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

If using a named SQL Server instance, adjust `Server=` accordingly (for example `localhost\\SQLEXPRESS`).

Do not hard-code production credentials in source code.

## V0.1 relationship model

```text
Brand
  1
  |
  ∞
Product
  ∞
  |
  1
Category

Product
  1
  |
  1
LaptopSpecification

UserProfile
  1
  |
  ∞
Favorite
  ∞
  |
  1
Product
```

---

## Brand

```text
Brand
- Id
- Name
```

Relationship:

```text
Brand 1 ───── ∞ Product
```

A brand has many products. Each product has one brand.

Suggested navigation:

```csharp
public ICollection<Product> Products { get; set; }
```

---

## Category

```text
Category
- Id
- Name
```

Relationship:

```text
Category 1 ───── ∞ Product
```

The V0.1 category is mainly `Laptop`, but the model remains extensible.

---

## Product

```text
Product
- Id
- Name
- Price
- Stock
- IsActive
- BrandId
- CategoryId
- CreatedAt
- UpdatedAt
```

Navigation targets:
- Brand
- Category
- LaptopSpecification
- Favorites

---

## LaptopSpecification

```text
LaptopSpecification
- Id
- Processor
- GPU
- RamGb
- StorageGb
- ScreenSize
- Resolution
- RefreshRate
- Weight
- OperatingSystem
- ProductId
```

Relationship:

```text
Product 1 ───── 1 LaptopSpecification
```

Each laptop product has one specification record. Each specification belongs to one product.

---

## UserProfile

If Supabase Auth is used, Pikwise does not store user passwords.

```text
UserProfile
- Id
- AuthProviderUserId
- Email
- Role
- CreatedAt
```

`AuthProviderUserId` maps the local profile to the authenticated provider user (`sub` claim).

---

## Favorite

Use an explicit join entity:

```text
Favorite
- UserProfileId
- ProductId
- CreatedAt
```

Relationship:

```text
UserProfile 1 ───── ∞ Favorite ∞ ───── 1 Product
```

Conceptually this is Many-to-Many between users and products, represented explicitly so we can store `CreatedAt`.

---

# Relationship configurations

## Brand -> Product

```csharp
modelBuilder.Entity<Brand>()
    .HasMany(b => b.Products)
    .WithOne(p => p.Brand)
    .HasForeignKey(p => p.BrandId);
```

## Category -> Product

```csharp
modelBuilder.Entity<Category>()
    .HasMany(c => c.Products)
    .WithOne(p => p.Category)
    .HasForeignKey(p => p.CategoryId);
```

## Product -> LaptopSpecification

```csharp
modelBuilder.Entity<Product>()
    .HasOne(p => p.LaptopSpecification)
    .WithOne(s => s.Product)
    .HasForeignKey<LaptopSpecification>(s => s.ProductId);
```

## Favorite join entity

```csharp
modelBuilder.Entity<Favorite>()
    .HasKey(f => new { f.UserProfileId, f.ProductId });

modelBuilder.Entity<Favorite>()
    .HasOne(f => f.UserProfile)
    .WithMany(u => u.Favorites)
    .HasForeignKey(f => f.UserProfileId);

modelBuilder.Entity<Favorite>()
    .HasOne(f => f.Product)
    .WithMany(p => p.Favorites)
    .HasForeignKey(f => f.ProductId);
```

---

# Foreign key vs navigation property

Example:

```csharp
public int BrandId { get; set; }   // Foreign Key
public Brand Brand { get; set; }   // Navigation Property
```

- Foreign Key stores the relationship at database level.
- Navigation property makes the related C# object reachable.

---

# Include / ThenInclude examples

Product + Brand:

```csharp
var product = await _context.Products
    .Include(p => p.Brand)
    .FirstOrDefaultAsync(p => p.Id == id);
```

Product + Brand + LaptopSpecification:

```csharp
var product = await _context.Products
    .Include(p => p.Brand)
    .Include(p => p.LaptopSpecification)
    .FirstOrDefaultAsync(p => p.Id == id);
```

User + Favorites + Products:

```csharp
var user = await _context.UserProfiles
    .Include(u => u.Favorites)
        .ThenInclude(f => f.Product)
    .FirstOrDefaultAsync(u => u.Id == id);
```

---

# Initial index / constraint direction

Likely useful:
- `Brand.Name` unique
- `Category.Name` unique
- `LaptopSpecification.ProductId` unique due to One-to-One
- `Favorite(UserProfileId, ProductId)` composite primary key
- `UserProfile.AuthProviderUserId` unique

Later filtering workload may justify indexes on:
- Price
- BrandId
- CategoryId
- RAM
- CPU/GPU fields

Do not add indexes blindly; use real query patterns.

---

# EF Core concepts to practice

This model intentionally covers:
- One-to-One
- One-to-Many
- Many-to-Many through explicit join entity
- Foreign keys
- Navigation properties
- Include
- ThenInclude
- Fluent API
- unique constraints
- indexes
- migrations
- async query methods

---

# Data rules
- Never store plaintext passwords.
- Do not expose entities directly when a response DTO is more appropriate.
- Use EF Core migrations for schema changes.
- Review generated migrations.
- Keep important relationships/index decisions documented.
- Recommendation scoring must use verified structured database fields.

## Session 2 — persistence setup

EF Core SQL Server and Design packages and the local dotnet-ef tool are pinned to
10.0.11. Restore the repository tool with `dotnet tool restore`.
ApplicationDbContext lives in Infrastructure/Persistence and currently has no
entities. Infrastructure registers it with the default scoped lifetime through
AddInfrastructure; the API invokes this method in its composition root.
The API also references Design with PrivateAssets=all because it is the tooling
startup project. Domain and Application remain independent of EF Core.

### Local connection configuration

Run from the repository root (adjust the server for your installation):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=PikwiseDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;" --project src/Pikwise.Api
```

This example uses Windows authentication and trusts the local development server
certificate. In deployment, provide a valid server certificate and do not enable
TrustServerCertificate. Supply production configuration through the deployment
secret store or the `ConnectionStrings__DefaultConnection` environment variable.
User Secrets are loaded in Development and are stored outside the repository;
they are a development convenience, not an encrypted production secret store.
No real connection string is stored in tracked appsettings files.
Missing or blank configuration stops startup with a configuration-key-only error.

### Verify tooling without a database connection

After configuring the connection and building:

```powershell
dotnet tool restore
dotnet build Pikwise.sln --configuration Release
dotnet ef dbcontext info --project src/Pikwise.Infrastructure --startup-project src/Pikwise.Api --configuration Release --no-build -- --environment Development
dotnet ef migrations list --no-connect --project src/Pikwise.Infrastructure --startup-project src/Pikwise.Api --configuration Release --no-build -- --environment Development
```

Session 2 verification: context info resolves the SQL Server provider and migration
listing reports no migrations. These commands do not prove server reachability.
No database was created or changed, and no real SQL connection was verified.

### Session 3 reference commands (not executed in Session 2)

After the real entities and relationships are implemented:

```powershell
dotnet ef migrations add InitialCreate --project src/Pikwise.Infrastructure --startup-project src/Pikwise.Api --output-dir Persistence/Migrations -- --environment Development
dotnet ef migrations script --project src/Pikwise.Infrastructure --startup-project src/Pikwise.Api -- --environment Development
# Review the generated migration and SQL before applying:
dotnet ef database update --project src/Pikwise.Infrastructure --startup-project src/Pikwise.Api -- --environment Development
```

The application does not call EnsureCreated or Migrate on startup.
