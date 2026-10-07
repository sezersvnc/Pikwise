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

## Session 3 — implemented schema and verification

Domain entities are mapped by Infrastructure/Persistence/Configurations.
The reviewed migration is `20261001173029_InitialCreate`; its generated SQL is
[InitialCreate.sql](InitialCreate.sql). The four business tables are Brands,
Categories, Products and LaptopSpecifications. EF also creates migration history.
The migration was applied to local SQL Server `localhost`, database `PikwiseDb`.
Windows authentication connection configuration was stored in User Secrets.

Foreign key placement:
- Product.BrandId: each product belongs to one brand, while a brand has many products.
- Product.CategoryId: each product belongs to one category, while a category has many products.
- LaptopSpecification.ProductId: specifications depend on products. Its unique index
  limits each product to at most one specification. The FK prevents orphan specifications.

Brand.Name and Category.Name have unique indexes. Brand/category deletion uses
NO ACTION and is rejected while products reference them. Deleting a product
cascades to its specification. Required relationships do not force every product
to have a specification; the future product creation use case must enforce that rule.
Navigation properties support object traversal and require explicit loading.

Price uses decimal(18,2); ScreenSize uses inches, decimal(5,2); Weight uses kg,
decimal(6,3). CreatedAt/UpdatedAt are datetimeoffset, with UpdatedAt nullable.
String lengths and reasons are recorded in ADR-011. Name uniqueness uses the
server/database collation. Application owns future value validation and timestamp
assignment; the database mapping does not invent those business rules.

### Relationship queries verified on SQL Server

```csharp
var product = await context.Products.AsNoTracking()
    .Include(p => p.Brand)
    .Include(p => p.Category)
    .Include(p => p.LaptopSpecification)
    .SingleAsync(p => p.Id == productId);

var brand = await context.Brands.AsNoTracking()
    .Include(b => b.Products)
    .ThenInclude(p => p.LaptopSpecification)
    .SingleAsync(b => b.Id == brandId);
```

Include loads one relationship level. ThenInclude continues from the previously
included relationship, here Brand -> Products -> LaptopSpecification.
These queries are exercised in SqlServerRelationshipTests; no product endpoint
or repository is introduced before Session 4.

### Test commands

Run fifty-one tests without SQL Server:

```powershell
dotnet test Pikwise.sln --configuration Release --filter 'Category!=SqlServer'
```

Run all fifty-seven tests against a dedicated local database:

```powershell
$env:PIKWISE_TEST_CONNECTION = 'Server=localhost;Database=PikwiseSession3Tests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;'
dotnet test Pikwise.sln --configuration Release
```

The SQL tests require the exact database name PikwiseSession3Tests. They apply
migrations and verify async writes/queries, unique name and specification
constraints, all three FKs, deletion behavior and the Session 4 HTTP endpoint.
Test records are removed after use; the empty migrated database remains for
subsequent runs. The migration and model snapshot have no pending differences.
Across the solution, fifty-seven tests pass: six unit tests and fifty-one integration tests.
Release build completes with zero errors and warnings.

Session 5 adds HTTP CRUD without changing the schema. The dedicated SQL test
database also verifies POST/PUT/DELETE, relation replacement, preserved creation
timestamps, invalid fields/references and specification cascade deletion. Cleanup
removes only the test's own records. No catalog data is inserted into PikwiseDb.

## Session 6 — user profiles and favorites

UserProfile and Favorite are Domain entities. Infrastructure owns their separate
Fluent API configurations and migration. Product now exposes a Favorites
navigation collection. UserProfile.Id remains a local integer identity; the
external identity is stored separately in AuthProviderUserId.

AuthProviderUserId is required Unicode text, limited to 128 characters, with a
unique index and Latin1_General_100_BIN2 collation. External subjects are treated
as case-sensitive identifiers even when the database's default collation is
case-insensitive. Email is required Unicode text up to 254 characters; Role is
required Unicode text up to 32 characters with a CLR default of User. Email has
no unique constraint because the external subject identifies the profile.
No passwords are stored. Role is not wired to authorization. Future profile and
favorite use cases must validate input and assign UTC CreatedAt timestamps;
the mapping supplies no SQL timestamp or role defaults.

Favorite has a composite primary key (UserProfileId, ProductId). Both foreign
keys live on Favorite because each favorite references exactly one profile and
one product. The key prevents a user from favoriting the same product twice,
including concurrent inserts. An explicit entity lets the relationship store
CreatedAt. The primary key supports queries starting with UserProfileId; the
separate ProductId index supports the reverse relationship.

Both Favorite foreign keys use ON DELETE CASCADE. Deleting a profile removes its
favorites while preserving products. Deleting a product removes its favorites
and its existing specification while preserving profiles. Deleting a favorite
does not delete either parent.

### Queries verified on real SQL Server

```csharp
var user = await context.UserProfiles.AsNoTracking()
    .Include(u => u.Favorites)
    .ThenInclude(f => f.Product)
    .ThenInclude(p => p.LaptopSpecification)
    .SingleAsync(u => u.Id == userId);

var product = await context.Products.AsNoTracking()
    .Include(p => p.Favorites)
    .ThenInclude(f => f.UserProfile)
    .SingleAsync(p => p.Id == productId);
```

Include loads the first relationship; each ThenInclude continues from that
relationship's target. FavoriteRelationshipTests verifies both directions,
timestamps, duplicate pair/subject rejection, case-sensitive subjects, orphan FK
rejection and database cascades without loading dependents. Test data is enclosed
in a transaction and rolled back in the dedicated test database.

Migration 20261001192501_AddUserProfilesAndFavorites adds only UserProfiles,
Favorites and their constraints/indexes. Its reviewed delta SQL is
[AddUserProfilesAndFavorites.sql](AddUserProfilesAndFavorites.sql). It was applied
to localhost/PikwiseDb, preserving existing business tables and data. Both
CASCADE constraints were checked in SQL Server; EF reports no pending model
changes. Authentication and profile/favorite endpoints remain future work.

## Session 7 — authenticated profile resolution

No schema change or migration is required. UserProfileRepository reads profiles
by AuthProviderUserId with AsNoTracking and no navigation loading. The protected
current-profile use case inserts a default User profile if none exists, using
validated sub/email claims and UTC CreatedAt. Existing local role/email values
are preserved. The LocalAdmin policy now reads the stored role.

Concurrent first requests are resolved by the unique subject index. SQL errors
2601/2627 trigger detachment of the failed insert and a new lookup for the same
subject. The test suite verifies this with concurrent HTTP requests and an explicit
duplicate insert on the dedicated database. No login credentials or real users
are inserted as test data in PikwiseDb. See AUTHENTICATION.md for the full flow.

## Session 8 — favorite operations

The existing Favorites composite key and relationships are reused without a new
migration. List queries filter by the resolved user Id, load product relations
with Include/ThenInclude and order by CreatedAt descending with ProductId as a
tie-breaker. Inserts set UTC CreatedAt in Application. Duplicate-key errors become
a controlled 409; FK errors from concurrent parent deletion also become 409.

Deletion is a single ExecuteDeleteAsync statement whose predicate contains both
UserProfileId and ProductId. It bypasses tracking/SaveChanges, affects at most one
row and preserves parents. Six integration tests require SQL Server, including
FavoriteApiSqlTests, which verifies the protected HTTP flow using two temporary
identities, concurrent insertion, isolation, timestamps, mapping and cascade
cleanup. Test data is removed from PikwiseSession3Tests after use.

## Session 9 — catalog reads

No schema/migration change. ProductRepository adds composable price/brand/spec
predicates, filtered CountAsync and explicit ordering with ID ties. Skip/Take
translates to SQL Server OFFSET/FETCH before materialization; includes load only
the page's response relationships. Count and page are separate statements without
snapshot consistency. Text predicates/order follow the database collation.

ProductQuerySqlTests verifies combined HTTP queries and records server SQL to
prove pagination executes in the database. The current suite has 74 passing tests
(9 unit, 65 integration): 7 require the guarded PikwiseSession3Tests database and
67 run without SQL. Query limits and review notes are in PRODUCT_QUERIES.md.

## Session 10 — comparison reads

No schema/migration change. GetByIdsAsync filters Products by the selected IDs
and loads Brand and optional LaptopSpecification in one AsNoTracking query.
The selection is bounded to 2-3 distinct IDs by Application validation. The
repository does not decide request completeness or output order; Service does.
No Category, Favorite or UserProfile relations are loaded.

ProductComparisonSqlTests verifies selected-only HTTP results, missing-ID errors,
optional specifications, one SQL command and no tracking. All 93 tests pass
(18 unit, 75 integration): 8 require the guarded PikwiseSession3Tests database
and 85 require no SQL. Test fixtures are cleaned up without changing PikwiseDb.
See COMPARISON.md for the contract and review guide.

---

## Session 11.5 — external reference data (Open Icecat)

### Schema changes (migration `Session115_ExternalReferencesAndNullableSpecs`, reviewed and applied to PikwiseDb)
- `LaptopSpecifications`: Processor, GPU, Resolution, OperatingSystem become nullable
  strings; RamGb, StorageGb, RefreshRate become `int?`; ScreenSize, Weight become
  `decimal?`. Column names, lengths and precisions are unchanged. Null means unknown.
- New table `ProductExternalReferences` (Id, ProductId FK cascade, Provider nvarchar(50),
  ExternalId nvarchar(100) with a binary collation, ImportedAt). Unique index on
  `(Provider, ExternalId)`; index on ProductId.
- `Products.Price` stays non-nullable.

### Data source and license
Specifications are imported from Open Icecat (https://icecat.biz) for development and
testing. Required obligations: attribute Icecat as the source; the data is under the Open
Content License (v1.4), which has share-alike terms and requires modifications to be
marked (Pikwise normalizes names and units, so imported values are modified data); fair-use
rate limits apply. The license prohibits using the data for machine learning/AI purposes,
so Icecat-derived data must NOT be used as generative-AI/LLM explanation input. Before
Session 15, get written permission from Icecat or use a different, suitably licensed source.

### Development price data
Open Icecat provides no price or stock. Price, Stock and IsActive come from a companion
CSV (`ExternalId,Price,Stock,IsActive`) and are **development/test data, not real market
data**. They must not be presented to users as such and the temporary layer is removed in
Session 13.

### Import tool
`tools/Pikwise.DataImport`: `discover` (stream index, write manifest), `inspect` (print raw
Icecat features of one product), `import` (dry run by default; `--apply` writes). Icecat
username/password are read from User Secrets or environment variables
(`Icecat__Username`, `Icecat__Password`), never from files in the repository.

### Dataset produced in Session 11.5
- Discovery: `discover --category-id 151 --exclude-suppliers 7,9,30492,41668` streamed ~7.8 million
  index entries. Icecat category 151 is laptops (its English name is not "Notebooks").
  Selection takes the 12 suppliers with the most laptop entries and their newest products
  (highest Product_ID), one entry per model name per supplier.
- Excluded suppliers: 9 (Apple) and 7 (Acer) return no or empty records in Open Icecat;
  30492 and 41668 are refurbishers that list other brands' products under their own name.
- The manifest was then reviewed by hand: two more refurbishers (Flex IT, upcycle it) and one
  record without hardware data were removed. The committed manifest holds 25 laptops.
- Result: 25 laptops, 10 brands (Lenovo, ASUS, Dell, HP, MSI, Fujitsu, Alienware, Dynabook,
  GIGABYTE, Samsung). Missing values: CPU 1, GPU 2, refresh rate 10, screen size 2.
  RAM 8-64 GB, storage 256-1000 GB, weight 0.828-2.54 kg, refresh rate 60-165 Hz.
- Known source issues kept as supplied: Fujitsu UQ-L1 lists an Intel GPU with a Snapdragon CPU;
  Icecat stores SKUs as Dell product names (e.g. "PW516265").
- `dev-prices.csv` holds illustrative TRY prices chosen for testing. One product is inactive and
  one has zero stock on purpose, to exercise recommendation eligibility. Not market data.
- Tool logs (`tools/Pikwise.DataImport/data/*.txt`) are local output and are not committed.
