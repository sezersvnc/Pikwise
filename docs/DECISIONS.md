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
**Status:** Accepted for Session 7; details in ADR-015

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

---

## ADR-012 — Session 4 product read slice
**Status:** Accepted

Keep `IProductRepository`, `IProductService`, response DTOs, mapper and service in
Application. Keep the EF Core repository implementation in Infrastructure and the
controller in API. The repository returns the Domain aggregate required by this
use case using an async, no-tracking query with Brand, Category and
LaptopSpecification included. The service maps it to an explicit response DTO.

Return 404 when no row matches. A missing LaptopSpecification remains `null` in
the response because the current relational model can enforce at most one
specification, while product completeness belongs to the future create use case.
Expose first-party ASP.NET Core OpenAPI JSON in Development and verify both the
documented responses and real HTTP behavior with automated integration tests.

---

## ADR-013 — Session 5 CRUD and validation
**Status:** Accepted

POST/PUT require a complete LaptopSpecification. Use separate create/update types
sharing editable fields; server-owned Id/CreatedAt/UpdatedAt are excluded. Service
checks Brand and Category references and sets UTC timestamps. PUT replaces editable
fields while preserving CreatedAt and updating the existing specification. DELETE
is physical and follows the reviewed cascade relationship. Writes persist in one
SaveChangesAsync call. No schema migration or lookup management endpoint is needed.

Use DataAnnotations plus a decimal scale attribute to avoid SQL rounding. Decimal
range constants parse with invariant culture, including on Turkish Windows.
Service validates the same contracts for callers outside MVC. HTTP validation uses
400, missing resources use 404, concurrent deletion/FK conflicts use 409 and
unexpected exceptions use a generic 500 with traceId. API owns the error mapping;
Infrastructure translates known persistence failures into an Application exception.
No rowversion is introduced: simultaneous updates currently use last-write-wins.
Authentication stays on its existing later-session schedule; writes are presently
local-development endpoints. GET all returns an unpaginated list ordered by Id.

---

## ADR-014 — Session 6 profiles and explicit favorite join
**Status:** Accepted

Use a local integer UserProfile.Id and a separate unique AuthProviderUserId.
Store the external subject as Unicode text up to 128 characters using
Latin1_General_100_BIN2 collation to preserve case-sensitive identity. Email is
limited to 254 characters and Role to 32; Role has a CLR default of User. Subject
uniqueness identifies profiles; email is not unique. No password is stored.

Favorite explicitly joins UserProfile and Product and stores CreatedAt. Its
(UserProfileId, ProductId) composite primary key enforces duplicate prevention
in SQL Server. Both FKs are required and cascade parent deletion to favorite
rows. Product deletion also retains its existing specification cascade. Parent
rows are preserved when the other parent or the join row is deleted. A separate
ProductId index supports inverse queries; the primary key starts with UserProfileId.

Domain remains EF-independent; Infrastructure owns mappings/migrations. DateTimeOffset
timestamps follow the existing model; future Application use cases must assign UTC
timestamps and validate profile/favorite input. No database defaults or auth routes
are added. Include/ThenInclude, uniqueness, orphan rejection and delete behavior
are verified against SQL Server. Stored Role does not yet authorize requests.

---

## ADR-015 — Session 7 Bearer validation and local profiles
**Status:** Accepted

Use Microsoft.AspNetCore.Authentication.JwtBearer 10.0.11 in API. Configure an
external HTTPS Supabase issuer and audience (default authenticated), validate
signature/issuer/audience/lifetime and allow ES256/RS256. Retrieve public JWKS
through IdentityModel's caching configuration manager; a provider-specific adapter
is necessary because the configured address serves JWKS directly. Do not introduce
passwords, token issuance or a legacy shared signing secret. Keep raw sub claims.

Reject tokens without one valid subject or provider role authenticated. API's
ICurrentUser implementation provides identity to Application without accepting a
client UserProfileId. Application provisions a local profile on first protected
access, requires a valid email for new profiles, sets UTC CreatedAt and Role=User.
Existing profiles retain their local fields. SQL's unique subject index arbitrates
concurrent inserts; Infrastructure retrieves the winner after duplicate errors.

Pikwise Admin access uses the local SQL role, not Supabase's database role or
user_metadata. Verify 401 and 403 separately with protected /api/auth/me and
/api/auth/admin-check. Product CRUD retains its current contract; Favorites API
remains Session 8. No schema migration or general role-management API is added.
JWT validation does not check session revocation per request; signing keys are
cached/refreshed and token lifetime has 30 seconds of clock skew. Configuration
and verification details are documented in AUTHENTICATION.md.
