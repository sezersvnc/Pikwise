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

## Session 4 implementation

`GET /api/products/{id}` is the first complete request path:

```text
ProductsController
  -> IProductService / ProductService
  -> IProductRepository / ProductRepository
  -> ApplicationDbContext / SQL Server
  -> ProductMapper
  -> ProductResponseDto
```

The controller owns routing and the 200/404 choice. ProductService coordinates
the repository call and mapping. IProductRepository is an Application abstraction;
its Infrastructure implementation owns the async EF Core query, `AsNoTracking`
and related-data loading. ProductMapper lives in Application because it maps the
Domain result into the use-case response contract. The API never returns an entity.

Registrations follow the dependency direction: Infrastructure registers the
repository implementation, while the API composition root registers the service.
The OpenAPI document is exposed only in Development.

## Session 5 implementation

Controllers route CRUD requests and select HTTP outcomes. Data annotations on
Application request DTOs validate shape, lengths, ranges and SQL decimal precision.
The service also runs the same validation for non-HTTP callers, verifies lookup
existence and manages timestamps. ProductMapper applies the input to an entity.
The repository owns tracking queries, lookup queries and SaveChangesAsync.
One SaveChanges call persists the product and specification atomically.

Read queries use AsNoTracking; update/delete use tracked entities. Updates modify
the existing specification instead of inserting a duplicate. GET all has a stable
Id order; filtering/pagination remain roadmap work. The API registers a central
IExceptionHandler with ProblemDetails. Infrastructure translates FK/concurrent
write failures into an Application exception without leaking EF dependencies.

## Application folder organization

Group code by feature, then by responsibility inside the feature. Namespaces match
the directories. Products uses this structure:

```text
Pikwise.Application/Products/
  DTOs/
  Interfaces/
  Services/
  Mappers/
  Validators/
  Exceptions/
```

This keeps product code together while making contracts, orchestration, mapping,
validation and error types easier to find. DecimalScaleAttribute belongs to
Validators. Repository implementations remain in Infrastructure. This organization
changes file locations and namespaces without changing HTTP or database behavior.

## Session 6 implementation

Domain owns UserProfile, Favorite and their navigation properties without EF or
HTTP dependencies. Infrastructure owns their Fluent API configurations, composite
key, indexes, delete behavior and migration. SQL integration tests exercise the
explicit join using Include/ThenInclude in both directions.

This session completes persistence relationships. Application/API gain no profile
or favorite use cases yet. Product responses continue to use explicit DTO mapping,
so the new navigation graph does not expose profile emails or roles. Authentication
and authorization require a later use case and pipeline integration.

## Session 7 implementation

API registers JWT Bearer validation and runs authentication before authorization.
SupabaseJwksRetriever adapts the provider's public key document to IdentityModel's
configuration cache. HttpCurrentUser implements Application's ICurrentUser using
validated claims. API owns protocol details; no JWT or HttpContext types enter
Application or Domain.

AuthController exposes protected current-profile and Admin-check endpoints.
UserProfileService resolves the subject, provisions a default User profile on first
access and returns an explicit DTO. Infrastructure's UserProfileRepository owns
async SQL access and duplicate-subject race recovery. LocalAdminHandler authorizes
against the SQL profile role, independent of provider roles/user metadata.
See AUTHENTICATION.md and ADR-015 for configuration, behavior and limits.

## Session 8 implementation

FavoritesController owns protected routes and HTTP outcomes. FavoriteService
reuses UserProfileService to derive ownership from validated identity, checks
product existence through IProductRepository and assigns UTC favorite timestamps.
Application's IFavoriteService never accepts a client user Id. FavoriteMapper
combines favorite metadata with the existing product response contract.

Infrastructure's FavoriteRepository filters reads/deletes by resolved UserProfileId,
loads only product relations needed by the mapper and handles SQL insert constraints.
The existing composite key rejects duplicates; Application maps duplicate results
to a FavoriteAlreadyExistsException and API returns 409. Missing local profiles
raise UserProfileUnavailableException mapped to 403. Owner-scoped ExecuteDeleteAsync
removes only the join row. No Domain/schema changes or new packages are needed.
See FAVORITES.md and ADR-016 for behavior and verification.

## Session 9 implementation

ProductsController binds query DTOs and returns a paged response. Application owns
query validation, page defaults/limits and metadata. ProductService reuses the
product mapper after retrieving one page. ProductPageResult carries entities and
the filtered count without exposing IQueryable outside Infrastructure.

ProductRepository composes EF filters, explicit ordering and Skip/Take in SQL,
then loads response relationships and materializes only the page. A separate
CountAsync statement supplies the filtered total. Nonunique sorts use ID as a
tie-breaker. No Domain/schema changes are required. See PRODUCT_QUERIES.md and
ADR-017 for the contract change, verification and concurrency limits.

## Session 10 implementation

ProductsController binds comparison IDs and documents HTTP 200/400/404.
ProductService validates the 2-3 distinct positive selections, calls one batch
repository method, requires every selected product to exist and restores request
order. ProductsNotFoundException carries missing IDs without HTTP dependencies;
API maps it to 404 ProblemDetails.

ProductRepository uses an awaited, untracked membership query with Brand and
LaptopSpecification loaded. ProductMapper reuses specification conversion for
detail and comparison DTOs. Category/favorite/profile loading is unnecessary.
Domain/schema stay unchanged. See COMPARISON.md and ADR-018.

## Session 11 design

Session 11 designed the recommendation engine without adding code. See
RECOMMENDATION_ENGINE.md, ADR-019 and ADR-021.

## Session 11.5 implementation

External laptop data enters through a provider abstraction. Application/ExternalProducts
owns the provider-neutral contracts and rules, grouped like the other features:

```text
Pikwise.Application/ExternalProducts/
  Interfaces/     IExternalLaptopProvider, ILaptopImportRepository
  Models/         ExternalLaptopRecord, PreparedLaptop, LaptopImportReport
  Services/       LaptopImportService, LaptopImportPreparer
  Normalization/  LaptopNormalizer, DevelopmentPriceCsvParser
```

Namespaces match the directories (for example
Pikwise.Application.ExternalProducts.Services). Infrastructure/ExternalProducts owns the Icecat specifics (IcecatLaptopProvider,
IcecatLaptopMapper, IcecatIndexDiscovery, IcecatOptions) and LaptopImportRepository,
which writes products and ProductExternalReferences through ApplicationDbContext.

Domain gains ProductExternalReference and nullable LaptopSpecification fields; it has
no Icecat types. The importer is the console tool tools/Pikwise.DataImport, which
references Application and Infrastructure; the API exposes no import endpoint. See
DATABASE.md and ADR-020.

## Session 12 implementation

RecommendationsController binds the POST body and returns 200/400; it holds no
scoring logic. RecommendationService validates the request (RecommendationValidationException,
mapped to 400 by the API), loads candidates through IRecommendationRepository and runs
RecommendationEngine. The engine and the LaptopPerformanceTiers table are static,
pure Application code: eligibility, hard constraints, normalization, weighting,
missing-data handling, rounding and tie-breaking are all there and unit tested
without EF Core. RecommendationMapper builds the response and reuses the product
specification DTO mapping.

Infrastructure's RecommendationRepository loads all products with Brand and
LaptopSpecification in one untracked query; it applies no business filter. Domain and
schema are unchanged; no migration or package was added. The LLM is not involved.
See RECOMMENDATION_ENGINE.md and ADR-022.

## Session 14 implementation

```text
Pikwise.Application/RequirementParsing/
  Interfaces/   IRequirementExtractor (provider abstraction), IRequirementParsingService
  DTOs/         NaturalLanguageRequestDto, ParsedRequirementsResponseDto
  Models/       ExtractedRequirements, RequirementExtractionContract (instructions + JSON Schema)
  Services/     RequirementParsingService
  Validators/   NaturalLanguageRequestValidator
  Exceptions/   RequirementExtractionInvalidException (502), RequirementExtractionUnavailableException (503)
Pikwise.Infrastructure/Llm/
  UnconfiguredRequirementExtractor (default until a provider is approved)
```

RecommendationsController exposes `POST /api/recommendations/criteria` and only binds
and returns. RequirementParsingService validates the text, calls the extractor with a
timeout, parses the output strictly, reuses RecommendationRequestValidator and applies the
digits-for-budget rule. It does not call the engine or the repository. ApiExceptionHandler
maps the two new exceptions and logs only their reason. No schema change, migration or
package. See AI.md and ADR-026.

## Session 15 implementation

```text
Pikwise.Application/Explanations/
  Interfaces/   IExplanationGenerator (provider abstraction), IExplanationService
  DTOs/         ExplanationResponseDto, ProductExplanationDto
  Models/       ExplanationInput, GeneratedExplanation, ExplanationContract (instructions, schema, input serialization)
  Mappers/      ExplanationMapper (recommendation response -> ExplanationInput)
  Services/     ExplanationService
  Validation/   ExplanationFactChecker
  Exceptions/   ExplanationInvalidException (502), ExplanationUnavailableException (503)
Pikwise.Infrastructure/Llm/
  UnconfiguredExplanationGenerator (default until a provider is approved)
```

RecommendationsController exposes `POST /api/recommendations/explanation` and only binds
and returns. ExplanationService depends on IRecommendationService (not on the repository
or the engine), so validation and ranking are exactly those of POST /api/recommendations.
It maps the ranking to ExplanationInput, calls the generator with a timeout, parses the
output strictly and runs ExplanationFactChecker before building the response. No schema
change, migration or package. See AI.md and ADR-027.
