# ARCHITECTURE.md — Pikwise V0.1

## Core request flow

```text
Client
  |
  | HTTP Request / JSON
  v
Controller
  |
  v
Application / Service
  |
  v
Repository Interface
  |
  v
Repository
  |
  v
DbContext / EF Core
  |
  v
SQL Server
```

Response:

```text
SQL Server
  -> Entity
  -> Repository
  -> Service
  -> Mapper
  -> Response DTO
  -> Controller
  -> JSON
  -> Client
```

## Layer responsibilities

### Pikwise.Api
Owns:
- routes,
- HTTP request/response,
- status codes,
- authentication/authorization pipeline,
- endpoint-level `[Authorize]`.

Must not own:
- EF Core queries,
- recommendation formulas,
- value calculations,
- domain orchestration.

### Pikwise.Application
Owns:
- use cases,
- business rules,
- business-state validation,
- orchestration,
- DTO contracts if kept here,
- recommendation logic coordination.

Examples:
- product active?
- product eligible?
- user may favorite?
- score calculation orchestration.

### Pikwise.Domain
Owns:
- core entities,
- domain concepts,
- business types that should not depend on EF Core or HTTP.

### Pikwise.Infrastructure
Owns:
- EF Core DbContext,
- repository implementations,
- SQL Server integration,
- Supabase/auth integration details,
- external service implementations.

## Dependency direction
Target direction:

```text
Api -> Application
Api -> Infrastructure (composition/DI only where needed)

Application -> Domain
Infrastructure -> Application abstractions
Infrastructure -> Domain
Domain -> nothing
```

Avoid circular dependencies.

## DTO / Mapper rules
- Request DTO: API accepts this shape.
- Entity: domain/persistence model.
- Response DTO: API exposes this shape.
- Mapper: C# object -> C# object.
- Model Binding: JSON -> C# request object.
- Serialization: C# response object -> JSON.

## Async rule
Use async for database/network I/O.

`async/await` does not make SQL inherently faster; it avoids unnecessarily blocking threads while waiting for I/O.

## Authentication direction
Preferred:
- Supabase Auth issues access tokens.
- ASP.NET Core validates incoming bearer tokens.
- Pikwise reads the authenticated user identity from claims.
- Pikwise owns authorization/business rules.

Authentication answers:
> Who are you?

Authorization answers:
> What may you do?

## Recommendation boundary

```text
Product DB = facts
Recommendation Engine = decision
LLM = understanding + explanation
```

## Session 1 implementation

The solution targets `net10.0` using the installed .NET SDK 10.0.200
(patch roll-forward is configured in `global.json`).

Project references follow the dependency direction above. The Api reference to
Infrastructure is reserved for composition/DI. Domain has no project or package
references. Application has no implementation yet. Infrastructure now contains the Session 2 persistence setup.

`GET /health` uses ASP.NET Core health checks registered through DI in the API
composition root. This is a liveness endpoint with no business logic, so it needs
no Application service or repository. No database readiness check is registered.
Integration tests exercise the actual HTTP pipeline using WebApplicationFactory.
The unit test project is prepared for future business rules and contains no tests yet.

## Session 2 implementation

Infrastructure owns ApplicationDbContext and AddInfrastructure(configuration).
API calls the registration method from Program.cs; no database queries live in API.
The context is scoped. Connection configuration is required at startup and comes
from User Secrets or environment variables. Session 3 adds the four core entities and InitialCreate migration.
See DATABASE.md for setup and tooling commands.

## Session 3 implementation

Domain holds Brand, Category, Product and LaptopSpecification without EF dependencies.
Infrastructure holds Fluent API mappings and migrations. SQL relationship queries
are verified in integration tests. API and Application gain no product use cases yet.
See DATABASE.md and ADR-011 for schema choices and relationship explanations.
