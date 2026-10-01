# DECISIONS.md — Pikwise ADRs

## ADR-001 — Laptop-only MVP
**Status:** Accepted

Start with laptops only.

Reason:
- narrows data modeling,
- makes scoring rules testable,
- avoids premature multi-category complexity.

---

## ADR-002 — Modular monolith first
**Status:** Accepted

Use:
- Pikwise.Api
- Pikwise.Application
- Pikwise.Domain
- Pikwise.Infrastructure

Do not start with microservices.

---

## ADR-003 — Business logic belongs in Application/Service
**Status:** Accepted

Controller:
- HTTP

Service:
- business logic / orchestration

Repository:
- data access

---

## ADR-004 — Deterministic Recommendation Engine
**Status:** Accepted

Recommendation ranking/scoring is deterministic and testable.

AI explains; engine decides.

---

## ADR-005 — Supabase Auth
**Status:** Proposed

Direction:
- Supabase handles registration/login/session/token issuance.
- ASP.NET Core validates bearer tokens.
- Pikwise owns authorization/business rules.
- local `UserProfile` may map to Supabase `sub`.

Review this before auth implementation.

---

## ADR-006 — V0.1 relational model
**Status:** Accepted

Relationships:
- Brand 1 -> many Product
- Category 1 -> many Product
- Product 1 -> 1 LaptopSpecification
- UserProfile -> Favorite <- Product using explicit join entity

Reason:
This covers the EF Core relationship types needed by the roadmap without overcomplicating V0.1.

---

## ADR-007 — Learn while building
**Status:** Accepted

Do not wait to finish every .NET topic before building Pikwise.

Use the roadmap as working vertical slices:
- encounter a need,
- learn the required concept,
- implement,
- review,
- continue.

The project is part of the learning process.

---

## ADR-008 — SQL Server
**Status:** Accepted

**Decision:** Use Microsoft SQL Server for Pikwise V0.1.

**Reason:** SQL Server is already available in the development environment, integrates directly with EF Core, and avoids introducing a second database system without a real project need.

**Provider:** `Microsoft.EntityFrameworkCore.SqlServer`

---

## ADR-009 — Session 1 runtime and health endpoint
**Status:** Accepted for the project skeleton

Use net10.0 and SDK 10.0.200, already installed in the development environment.
Pin the SDK feature band with latestPatch roll-forward for reproducible builds.
Use xUnit for the two test projects and WebApplicationFactory for API integration tests.
Use built-in ASP.NET Core health checks at /health to verify startup, DI and HTTP
routing without introducing business services or persistence ahead of their sessions.

## ADR-010 — Session 2 persistence composition and configuration
**Status:** Accepted

Keep ApplicationDbContext and SQL Server registration in Infrastructure. API calls
AddInfrastructure and serves as the EF tooling startup project. Use the installed
EF tool version 10.0.11 for both packages and a repository-local tool manifest.
Use scoped DbContext lifetime. Require external connection configuration through
User Secrets in Development or environment/deployment secrets. Fail early for a
missing connection string without logging its value. Keep sensitive-data logging
disabled. No design-time factory is needed: tooling uses the same host registration.
Health remains liveness-only. Entities and initial migration belong to Session 3.

## ADR-011 — Session 3 relational mapping
**Status:** Accepted

Brand, Category, Product and LaptopSpecification are EF-independent Domain entities.
Use separate IEntityTypeConfiguration classes in Infrastructure. Keep the documented
integer Id on LaptopSpecification and enforce a unique, required ProductId FK.
BrandId and CategoryId are required Product FKs with NO ACTION on deletion; deleting
a referenced lookup must not remove products. Product deletion cascades to its
owned specification. A FK cannot require every product to have a specification;
that completeness rule belongs to the future Application product creation use case.

Use required Unicode names with limits of 100 characters for lookups and 200 for
products, CPU and GPU; resolution is 50 and OS is 100. Use decimal(18,2) for price,
decimal(5,2) for screen inches, decimal(6,3) for weight in kg. Timestamps use
DateTimeOffset; UpdatedAt is nullable until the first update. The future use case
owns timestamp assignment. Name uniqueness follows the configured SQL Server
collation. No speculative filtering indexes, seed catalog or Favorite model is added.
