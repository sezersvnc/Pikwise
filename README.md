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
EF Core relationship concepts    🟡 continue in project
MD architecture                  ✅
Project skeleton                 ✅ Session 1 complete
Product system                   next
```

## Read order
1. `docs/PROJECT.md`
2. `docs/ARCHITECTURE.md`
3. `docs/DATABASE.md`
4. `docs/API.md`
5. `docs/ROADMAP.md`
6. `TASKS.md`
7. `docs/DECISIONS.md`

## Run the Session 1 skeleton

Prerequisite: .NET SDK 10.0.200 (or a newer patch in the same feature band).

```powershell
dotnet restore Pikwise.sln
dotnet build Pikwise.sln --configuration Release --no-restore
dotnet test Pikwise.sln --configuration Release --no-build
dotnet run --project src/Pikwise.Api --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080/health: expected HTTP 200, body `Healthy`.
No database connection or credentials are required for Session 1.
The unit test project is intentionally empty until business rules are implemented;
the integration test verifies startup, DI, routing and the health response.
