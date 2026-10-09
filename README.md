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
Product comparison               ✅ Session 10 complete and pushed
Recommendation engine design     ✅ Session 11 complete
Real laptop dataset (Icecat)     ✅ Session 11.5 complete (25 laptops, dev prices)
Recommendation Engine V1         ✅ Session 12 complete (POST /api/recommendations)
Value-for-money analysis         ✅ Session 13 complete (valueAnalysis in recommendations)
LLM structured input             ✅ Session 14 complete (criteria endpoint, no provider yet)
AI explanation                   ✅ Session 15 complete (explanation endpoint)
LLM provider (Groq, free + ZDR)  ✅ Session 15.5 complete (auth + rate limit; smoke tested)
Frontend readiness               ✅ Session 15.6 complete (CORS, Admin-only product writes)
```

## Read order
1. `docs/PROJECT.md`
2. `docs/ARCHITECTURE.md`
3. `docs/DATABASE.md`
4. `docs/API.md`
5. `docs/ROADMAP.md`
6. `TASKS.md`
7. `docs/DECISIONS.md`
8. `docs/FRONTEND.md` (frontend requirements and API usage for the frontend teammate)

## Run the backend

Prerequisite: .NET SDK 10.0.200 (or a newer patch in the same feature band).

```bat
dotnet tool restore
dotnet restore Pikwise.sln
dotnet build Pikwise.sln --configuration Release --no-restore
dotnet test Pikwise.sln --configuration Release --no-build --filter "Category!=SqlServer"
dotnet run --project src/Pikwise.Api --no-launch-profile -- --environment Development --urls http://localhost:5080
```

The commands work in both CMD and PowerShell.
Open http://localhost:5080/health: expected HTTP 200, body `Healthy`. Without
`--no-launch-profile`, the `http` launch profile serves the API on http://localhost:5157.
Before running, configure ConnectionStrings:DefaultConnection using the User Secrets command in [docs/DATABASE.md](docs/DATABASE.md). A running database is not required for /health.
Also configure Authentication:Supabase:Issuer using [docs/AUTHENTICATION.md](docs/AUTHENTICATION.md).
241 tests run without SQL Server (160 unit, 81 integration). Nine additional integration
tests (`Category=SqlServer`) verify the migrations, relationships, HTTP CRUD, favorites, catalog
queries, comparisons and recommendations on a dedicated SQL Server database; see docs/DATABASE.md for the command. Request examples and error contracts
are in docs/API.md. Product CRUD retains its public local-development contract.

Session 6 adds UserProfiles and Favorites with a composite favorite key, explicit
foreign keys and tested Include/ThenInclude queries. The reviewed migration is
applied to local PikwiseDb. Session 7 adds protected /api/auth/me and
/api/auth/admin-check endpoints, validates Supabase JWTs and resolves local
profiles from sub claims. Session 8 adds protected GET/POST/DELETE favorites,
owner isolation and controlled duplicate handling. Session 9 adds combined catalog
filters, explicit sorting and SQL pagination with metadata. Session 10 adds structured
comparison of two or three selected products in one SQL read.
GET /api/products now returns an object with items instead of a bare array.
Session 9 was reviewed and pushed. Session 10 was approved for commit/push;
Session 11 (recommendation design) is complete; see docs/RECOMMENDATION_ENGINE.md. Session 12 implements the deterministic engine as `POST /api/recommendations` (Top 3 with score components; no LLM). Session 13 adds `valueAnalysis` (best fit vs best value, cheaper near-equal alternatives within 3 points, price per point); see docs/API.md. Session 14 adds `POST /api/recommendations/criteria` (natural language -> validated criteria behind a provider abstraction); see docs/AI.md. Session 15 adds `POST /api/recommendations/explanation` (ranks first, then explains the Top 3 with a deterministic fact check). Session 15.5 connects both to Groq's free plan with Zero Data Retention; they require a signed-in user and are rate limited (ADR-028). To enable them locally, store your own Groq key with `dotnet user-secrets set "Groq:ApiKey" <key> --project src/Pikwise.Api`; without it both answer 503. Session 11.5 imported 25 real laptop specifications from Open Icecat with `tools/Pikwise.DataImport` (prices are development/test data); see docs/DATABASE.md. Details are in [docs/COMPARISON.md](docs/COMPARISON.md),
[docs/PRODUCT_QUERIES.md](docs/PRODUCT_QUERIES.md)
and [docs/Pikwise.http](docs/Pikwise.http).
