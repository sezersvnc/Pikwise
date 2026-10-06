# Pikwise

Pikwise is a product-comparison and recommendation platform.

## V0.1 focus
Laptop category only.

The system should:
- store normalized laptop data,
- expose product CRUD,
- support filtering and comparison,
- calculate deterministic recommendation scores,
- evaluate value-for-money,
- later use AI to explain recommendations,
- keep product facts and recommendation logic outside the LLM.

## Tech direction
- Backend: ASP.NET Core Web API
- ORM: Entity Framework Core
- Database: SQL Server
- Auth: Supabase Auth is the preferred direction
- Architecture: modular monolith with clear layers

## Solution shape

```text
src/
  Pikwise.Api/
  Pikwise.Application/
  Pikwise.Domain/
  Pikwise.Infrastructure/

tests/
  Pikwise.UnitTests/
  Pikwise.IntegrationTests/
```

## Current position

```text
.NET architecture fundamentals   ✅
Service/repository separation    ✅
DTO/Mapper/DI/async concepts     ✅
Basic JWT architecture           ✅
EF Core relationship concepts    ✅ Session 6 complete
MD architecture                  ✅
Project skeleton                 ✅ Session 1 complete
Product model                    ✅ Session 3 complete
GET product by id                ✅ Session 4 complete
Complete product CRUD            ✅ Session 5 complete
User profiles / favorites        ✅ Session 6 complete
Authentication foundation        ✅ Session 7 complete
Authenticated favorites API      ✅ Session 8 complete
Filtering / sorting / pagination ✅ Session 9 complete and pushed
Product comparison               ✅ Session 10 implemented; commit/push approved
```

## Read order
1. `docs/PROJECT.md`
2. `docs/ARCHITECTURE.md`
3. `docs/DATABASE.md`
4. `docs/API.md`
5. `docs/ROADMAP.md`
6. `TASKS.md`
7. `docs/DECISIONS.md`

## Run the backend (Session 10)

Prerequisite: .NET SDK 10.0.200 (or a newer patch in the same feature band).

```powershell
dotnet tool restore
dotnet restore Pikwise.sln
dotnet build Pikwise.sln --configuration Release --no-restore
dotnet test Pikwise.sln --configuration Release --no-build --filter 'Category!=SqlServer'
dotnet run --project src/Pikwise.Api --no-launch-profile -- --environment Development --urls http://localhost:5080
```

Open http://localhost:5080/health: expected HTTP 200, body `Healthy`.
Before running, configure ConnectionStrings:DefaultConnection using the User Secrets command in [docs/DATABASE.md](docs/DATABASE.md). A running database is not required for /health.
Also configure Authentication:Supabase:Issuer using [docs/AUTHENTICATION.md](docs/AUTHENTICATION.md).
Eighty-five tests run without SQL Server. Eight additional integration tests verify
the migration, relationships, HTTP CRUD, catalog queries and comparisons on a dedicated SQL Server
database; see docs/DATABASE.md for the command. Request examples and error contracts
are in docs/API.md. Product CRUD retains its public local-development contract.

Session 6 adds UserProfiles and Favorites with a composite favorite key, explicit
foreign keys and tested Include/ThenInclude queries. The reviewed migration is
applied to local PikwiseDb. Session 7 adds protected /api/auth/me and
/api/auth/admin-check endpoints, validates Supabase JWTs and resolves local
profiles from sub claims. Session 8 adds protected GET/POST/DELETE favorites,
owner isolation and controlled duplicate handling. Session 9 adds combined catalog
filters, explicit sorting and SQL pagination with metadata. Session 10 adds structured
comparison of two or three selected products in one SQL read. All 93 tests pass.
GET /api/products now returns an object with items instead of a bare array.
Session 9 was reviewed and pushed. Session 10 was approved for commit/push;
Session 11 (recommendation design) is in progress and documentation-only; see docs/RECOMMENDATION_ENGINE.md. Details are in [docs/COMPARISON.md](docs/COMPARISON.md),
[docs/PRODUCT_QUERIES.md](docs/PRODUCT_QUERIES.md)
and [docs/Pikwise.http](docs/Pikwise.http).
