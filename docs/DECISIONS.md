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

---

## ADR-016 — Session 8 authenticated favorites
**Status:** Accepted

Use three protected routes: GET /api/favorites, POST /api/favorites/{productId}
and DELETE /api/favorites/{productId}. Resolve local ownership through the existing
current-profile service. The client supplies only a positive product Id; no body
is required. FavoriteResponseDto contains CreatedAt and the existing product DTO.
Application owns product-existence checks, UTC timestamps and duplicate behavior;
Infrastructure owns queries/inserts/deletes. Reuse product lookup/mapping instead
of adding another product contract. No schema change is needed.

Return 201 for new favorites, 409 for duplicates and concurrent FK write conflicts,
404 for missing products on insertion, and 204/404 for successful/absent owner-scoped
deletion. A duplicate preserves the original favorite's creation time. Any existing
product is eligible, including inactive/out-of-stock ones. New availability rules
require a later explicit decision.

Filter list/delete SQL by the resolved user Id. Lists use AsNoTracking with the
product's Brand, Category and specification loaded; order by favorite CreatedAt
descending, then ProductId ascending. Delete the composite pair with ExecuteDeleteAsync
and use affected-row count to determine existence. Zero rows never reveals whether
another user's favorite exists. Parent rows are preserved. The composite primary
key, rather than a pre-insert existence check, arbitrates simultaneous inserts.

No paging/filtering work is included. Session 9 remains separate. HTTP and SQL
tests verify protection, ownership, DTO output, duplicates, missing/invalid IDs,
concurrent inserts and parent/cascade behavior. See FAVORITES.md.

---

## ADR-017 — Session 9 catalog filtering and pagination
**Status:** Accepted

Replace GET /api/products's array with items/page/pageSize/totalCount/totalPages/
hasPreviousPage/hasNextPage. Reuse ProductResponseDto for items. Default to page 1,
20 items, ID ascending; reject sizes outside 1..100 and offsets exceeding Int32.
Application owns validation and metadata; Infrastructure owns IQueryable and EF
execution. API owns query binding and HTTP outcomes. Keep service validation for
callers outside MVC. No new packages, Domain changes or migrations are needed.

Combine inclusive min/max prices, brand ID, minimum RAM/storage and trimmed CPU/
GPU substring filters with AND. Preserve inactive/out-of-stock catalog visibility.
Specification filters exclude missing specs; no filter requires specs implicitly.
Allow id/price/name/ram/createdAt ordering and asc/desc using ordinal comparison.
Append ID ascending for ties. Text matching and NULL ordering follow SQL Server.

Execute filtered CountAsync, then sorted Skip/Take/Include/ToListAsync with awaited
cancellation. Separate statements can observe concurrent writes; no snapshot
consistency is promised. Offset and substring costs may require measured future
optimization. Favorites keep their Session 8 response. Session 10 comparison is
outside this change. See PRODUCT_QUERIES.md for implementation and verification.

---

## ADR-018 — Session 10 structured product comparison
**Status:** Accepted

Expose public GET /api/products/compare with repeated ids parameters. Accept
exactly 2 or 3 distinct positive Int32 IDs; reject duplicates/count/shape errors
with 400. Preserve input order in the products response. If any product is
absent, return 404 with all missingProductIds in input order and no partial result.
Application owns these rules; the API owns HTTP translation.

Reuse IProductService/ProductService for the product use case and add repository
GetByIdsAsync. Fetch the selection in one AsNoTracking membership query with Brand
and LaptopSpecification; do not perform per-ID reads. Service restores ordering
after materialization. Return dedicated comparison response/item DTOs with id,
name, price, compact brand and the existing LaptopSpecificationDto. Share
specification mapping with product detail. Omit unrelated fields and user data.

Missing specifications remain null; stored availability does not restrict this
read. Facts are returned without inferred specs, deltas, scores, ranking or LLM.
No schema/migration, packages or new DI registrations are required. Recommendation
design remains Session 11. See COMPARISON.md for verification and review.

---

## ADR-019 — Session 11 recommendation engine design
**Status:** Accepted; completed by ADR-021

The engine is a deterministic pipeline: eligibility and hard filters, fixed-range
normalization, weighted scoring, ranking, Top 3. Products with IsActive=false or
Stock=0 are not recommended; this does not change catalog, favorites or comparison.
Hard constraints are strict eliminations with no tolerance.

Normalization maps each criterion through a documented fixed reference range with
clamping, so scores do not change when the catalog changes. Lower-is-better criteria
use the inverted value. Component scores are 0..1; the displayed score is 0..100.
Each criterion receives an importance level 1..5, defaulting to 3, and weights are
the level divided by the sum of levels of the criteria scored for that product.

Unknown values are excluded from that product's score and the remaining weights are
rescaled; the result reports the unknown criteria and the known-weight share. No
value is guessed and no penalty is applied. CPU and GPU free text is scored through a
manually maintained tier table; unlisted models are unknown. Ties are broken by price
ascending, then product Id ascending.

The V1 scored criteria are RAM, storage, CPU tier, GPU tier, weight and refresh rate.
Supported hard constraints are budget ceiling, minimum RAM and storage, and maximum
weight. Price is only a hard filter in V1; price/performance analysis is a core
planned capability for Session 13 and stays separate from the fit score. A product
whose field is unknown for an active hard constraint is removed. A usage type is not
part of UserRequirements; the LLM step (Session 14) maps described needs to
importance levels and constraints. The remaining constants are in ADR-021.

---

## ADR-020 — Session 11.5 external product data provider and Open Icecat
**Status:** Accepted for development/test data only

Real laptop specifications enter Pikwise through a provider abstraction:
`Open Icecat -> Icecat DTO/JSON -> Mapper/Normalizer -> Pikwise domain model -> SQL Server`.
Application defines IExternalLaptopProvider and a provider-neutral ExternalLaptopRecord;
Infrastructure holds the Icecat client, discovery and mapper; Domain and the
recommendation engine never see Icecat. The importer is a console tool
(tools/Pikwise.DataImport), not an API endpoint. It runs as a dry run by default and
writes to the database only with --apply.

Specification columns become nullable (unknown stays null, nothing is guessed).
Product.Price stays non-nullable. Open Icecat has no price or stock, so these come from
a companion CSV that is development/test data, not market data, and is removed in
Session 13. Provider links live in ProductExternalReferences with a unique
(Provider, ExternalId); Product gets no Icecat-specific fields and no Model column
(Name is "Brand Model"). Discovery streams the Icecat index only to select ~20-30
notebook IDs, saved in a manifest; the catalog is not imported.

Licensing: Open Icecat is accepted as a Session 11.5 dev/test spec source under the
Open Content License (attribution, share-alike, modifications marked). Using
Icecat-derived data as input to generative AI/LLM explanations is NOT approved (license
article 10). Before Session 15, obtain written permission from Icecat or use another
suitably licensed source. Credentials live only in User Secrets or environment
variables.

---

## ADR-021 — Session 11 completion: ranges, tiers and thresholds
**Status:** Accepted

Chosen after reviewing the 25 laptops imported in Session 11.5. Reference ranges:
RAM 8-32 GB, storage 256-1024 GB, weight 1.0-2.5 kg (lower is better), refresh rate
60-165 Hz; values are clamped. CPU and GPU are mapped to five tiers through a
hand-maintained table in code (Application layer, unit tested), matched by exact
case-insensitive name; tier n = (tier - 1) / 4. Unlisted or ambiguous names (for
example "Intel Graphics") are unknown. A product with less than 50% of the total
importance known is not recommended. Scores use decimal arithmetic, are rounded
half-up to two decimals and compared after rounding. All UserRequirements hard
constraints are optional; importance levels are 1-5 with default 3.

The full tables and a hand-verified ranking on real data are in
RECOMMENDATION_ENGINE.md. Implementation is Session 12.

---

## ADR-022 — Session 12 Recommendation Engine V1 implementation
**Status:** Accepted

Implements ADR-019 and ADR-021 without changing any rule, range, tier or threshold.
Public `POST /api/recommendations` takes the optional UserRequirements as a JSON body
(POST because the structured body does not fit a query string; the request has no
side effects). Request limits: budgetMax 0.01..10,000,000 with 2 decimals, minRamGb
1..256, minStorageGb 1..16384, maxWeightKg 0.1..10 with 2 decimals, importance 1..5.

The engine is a static, pure Application function (RecommendationEngine) with the CPU/GPU
table in LaptopPerformanceTiers. The repository loads every product with Brand and
LaptopSpecification in one AsNoTracking query; eligibility and hard constraints are
applied in Application, keeping all rules in one unit-tested place. Moving filters to
SQL is a later decision if the catalog grows.

The score uses full decimal precision and is rounded half-up to 2 decimals; component
values are rounded to 4 decimals for display only. The 50% known-share threshold compares
integer importance sums, so exactly 50% qualifies. The response returns at most three
items with components, unknown criteria and known importance, plus a summary of how many
products each rule removed. Unit tests reproduce the Session 11 hand ranking on the 25
Session 11.5 laptops, including HP EliteBook 6 G1i in 5th place on 12/18 known importance.
No schema change, migration or package was added. Value-for-money remains Session 13.

---

## ADR-023 — Future decision: where CPU/GPU tiers live when more categories arrive
**Status:** Deferred (no code change); decide when the second product category starts

Context: the CPU/GPU tier table is a hand-maintained static class in Application
(ADR-021). Processor and GPU are free text, so some source must map a name to a
performance level. Roadmap rule: do not generalize before a second category exists.

Options considered:
- A. Keep a code table per category (current; simple, needs a deploy per change).
- B. Database table (Category, Component, Name, Tier), seeded with the current laptop
  table; no deploy to add models; one mechanism for every category. Preferred when the
  need is real.
- C. Numeric benchmark score per component instead of a tier, normalized with a fixed
  range like RAM or weight. Finer than five tiers, but needs a reliable, licensed data
  source (see the Icecat license lesson in ADR-020).
- D. An LLM assigns tiers. Rejected: product facts and ranking inputs must not come from
  an LLM (ADR-004).

Rules that hold for any option: every tier or score is a recorded human decision, the
engine only reads it and never guesses, and an unlisted name stays unknown. Numeric
criteria (RAM, storage, weight, refresh rate) already follow a generic value/range/direction
pattern and can be configured per category later. A second category also needs its own
specification model (see TASKS.md "Additional Product Categories"). Revisit then, record a
new ADR and do not change the laptop behavior without an owner decision.

---

## ADR-024 — Session 13 value-for-money analysis
**Status:** Accepted

The fit ranking (ADR-019/021/022) is unchanged; price/performance is reported beside it.
A pure Application function (ValueAnalyzer) reads the engine's full ranking. The best fit
is rank 1. Every other ranked product that is cheaper and at most 3.00 points lower
(rounded scores) is a near-equal cheaper alternative, also when it ranks below the Top 3.
Each alternative reports score gap, price difference and price per point (half-up, 2
decimals). isSmallGainUpgrade is true when an alternative exists. Best value is the
cheapest of the best fit and its alternatives (ties: higher score, then lower Id).
The result is returned as `valueAnalysis` in the existing POST /api/recommendations
response; null when nothing qualifies. Percentage rules and a TL-per-point ceiling were
considered and not chosen. No schema change, migration, repository or controller change.

The development price CSV layer stays. ADR-020 planned to remove it in Session 13, but value
analysis needs prices and no real price source exists before the post-MVP store/price
tracking work. It is removed when a real, suitably licensed price source replaces it. Test
prices must never be shown to users as market prices, so value results on the Session 11.5
dataset are for development and testing only.

---

## ADR-025 — Icecat permission for LLM explanations; price-source plan
**Status:** Accepted (updates the LLM restriction in ADR-020)

Icecat permission (written, e-mail of 7 October 2026, from Icecat NV's Global Business
Development Manager; the owner keeps the correspondence as a PDF outside the repository):
- Open Icecat data may be used as described for Pikwise, a non-commercial student project,
  as long as it is not used to train an AI model or for similar AI functions.
- After clarification that product specifications are sent to an LLM only as request-time
  context to write a "why this laptop fits you" explanation, never used for training or
  fine-tuning and never stored, Icecat answered that this use is acceptable.
- Open Icecat data may also be used if the project becomes commercial; Full Icecat
  content requires a paid subscription.
- Icecat stated that attribution is not mandatory. Pikwise keeps attributing Icecat
  anyway, for transparency.
- Icecat Stock and Pricing only updates prices from the customer's own suppliers, so it is
  not a price source for Pikwise.

Conditions Pikwise must keep (Session 15 and later):
- Icecat data goes to an LLM only as request-time input for explanations or summaries.
  No training, fine-tuning, embedding store or bulk transfer of Icecat data.
- The LLM provider must be configured so submitted data is not retained or used for
  training (zero data retention or the provider's equivalent). Verify this before the
  first real call.
- Any other use of Icecat data with AI needs a new question to Icecat.

Price-source plan (no open-licensed laptop price source exists; researched 2026-10-07):
- Until Session 22: development prices from dev-prices.csv, shown as demo prices.
- Session 22 (Store / ProductOffer): manually entered real prices per offer with store,
  source URL and observed date; displayed with that date.
- Later: store feeds and affiliate feeds as providers behind the provider abstraction.
- Amazon Creators API (amazon.com.tr) is not usable now: it needs an approved Associates
  account with 10 sales in the trailing 30 days, allows at most 24-hour caching (no price
  history) and forbids use with generative AI. eBay restricts AI use; Best Buy is US/USD.
  Revisit when Pikwise is live.

---

## ADR-026 — Session 14 natural-language criteria (LLM structured input)
**Status:** Accepted

`POST /api/recommendations/criteria` turns the user's text into criteria in the
POST /api/recommendations request shape. It returns criteria only; ranking stays a
separate deterministic call, so the language model can never choose or reorder products.

- Provider abstraction: `IRequirementExtractor` in Application (text in, raw JSON out).
  Provider adapters belong in Infrastructure/Llm. The schema and instructions
  (`RequirementExtractionContract`) are provider-neutral Application code.
- Only the user's text is sent. No product (Icecat) data is part of this request, so the
  ADR-025 conditions apply to Session 15, not here.
- Output is untrusted: strict JSON (unknown or duplicate fields and string numbers
  rejected), then the same validator as a manual request. Invalid output is rejected
  with 502; values are never clamped. A budget is accepted only when the text contains
  digits, so the model cannot invent a price limit. Uncovered wishes are listed in
  `unsupported`.
- Timeout 15 s (also enforced when a provider ignores cancellation); provider failure
  or timeout answers 503. Text is limited to 1000 characters and is never logged.
- No real provider in Session 14 (option (a)): the owner is a student and wants no paid
  calls. `UnconfiguredRequirementExtractor` answers 503; tests use a fake extractor.
  No package, API key or configuration was added.
- The endpoint is anonymous while it costs nothing. Before a real provider is enabled
  (separate owner approval): require authentication, add per-user rate limiting, check
  the provider's data-retention and training terms, and keep the key in User Secrets or
  environment variables.

Known limitation: a budget written only in words ("elli bin") is not accepted; the
instructions ask the model to report it in `unsupported` instead.

---

## ADR-027 — Session 15 AI explanation
**Status:** Accepted

`POST /api/recommendations/explanation` takes the same criteria as POST
/api/recommendations, ranks through IRecommendationService first and then asks a
language model to explain that ranking. The response carries the ranking and the
explanation together, so they cannot drift apart if data changes between two calls.

- Separate endpoint: the recommendation endpoint stays fast, free and deterministic;
  a model failure (503/502) never breaks recommendations; explanations cost only when
  requested; authentication and rate limiting can later apply to this endpoint alone.
- Model input: the user's criteria, candidate/ranked counts, Top 3 stored facts, scores,
  score components (value, contribution) and valueAnalysis. No other products, no
  normalized values or weights. Null facts are unknown and must be called unavailable.
- Provider abstraction `IExplanationGenerator` (input JSON in, raw JSON out); the
  instructions, schema and input serialization are provider-neutral Application code.
- Output is untrusted: strict JSON, then a deterministic fact check. Every Top 3 product
  exactly once and no other id; a value comment only with a value analysis; texts of
  1..1200 characters; every number in a text must appear in the input (Turkish and
  English separators, "bin"/"milyon", rounding to 0-2 decimals accepted). Any failure
  rejects the whole explanation with 502 (no partial explanations).
- Known limits: numbers written as words and invented facts without digits are not
  detected, and numbers are checked against the whole input, not per product.
- Top 3 stays (ADR-019/022). A request `limit` (1..5, default 3) was discussed and left for
  a separate decision.
- No real provider in Session 15 (option (a)). `UnconfiguredExplanationGenerator` answers
  503; tests use fakes. The owner will choose the provider. Before it is enabled: owner
  approval, authentication and rate limiting (ADR-026), and the ADR-025 Icecat
  conditions (zero data retention, no training, request-time input only), because this
  is the first feature that sends Icecat-sourced facts to a model.

---

## ADR-028 — Session 15.5 Groq as the language model provider
**Status:** Accepted (implements the provider conditions of ADR-025, ADR-026 and ADR-027)

Groq is the first real provider behind IRequirementExtractor and IExplanationGenerator.
Researched 2026-10-08 against the alternatives: Gemini's free tier uses data to improve
products (human review), OpenRouter's free models log prompts for training, Mistral's free
plan trains by default, GitHub Models is retired, and Ollama (local) needs hardware the
deployment would also need. Groq's free plan never charges (429 at the limit), does not
train on API data and offers Zero Data Retention to every customer.

- The owner enabled organization-wide ZDR on 8 October 2026 and created the key; the
  account has no payment details. The key is stored only in User Secrets / environment
  (`Groq:ApiKey`); it is never committed, printed or logged.
- Model `openai/gpt-oss-120b` (strict json_schema support), reasoning effort low,
  reasoning excluded, max 2048 completion tokens; all configurable under `Groq`.
- Infrastructure/Llm owns GroqChatClient (typed HttpClient, Authorization header redacted
  in logs, only status codes logged) and two adapters. Any HTTP error, network error or
  unreadable response becomes the feature's "unavailable" exception (503).
- Without a key the unconfigured implementations stay registered; integration tests
  clear the key so they never reach Groq.
- ADR-026 conditions: both language model endpoints require a Supabase user token and
  share a rate limit of 5 requests per user and 25 in total per minute (configurable
  under `RateLimiting:LanguageModel`), answered with 429 problem details and Retry-After.
  The recommendation endpoint stays public and unlimited.
- Package `Microsoft.Extensions.Http` 10.0.11 added to Infrastructure.
- Free-plan limits (per model, also tokens per minute) make this a development/MVP
  setup; real traffic needs a paid plan or another provider, which is one new adapter.

---

## ADR-029 — Session 15.6 CORS for the frontend and Admin-only product writes
**Status:** Accepted

The frontend is a separate browser app built by a teammate (docs/FRONTEND.md), so the
API needs CORS, and the product write endpoints, public since Session 5 as a development
contract, must not stay open once a public frontend exists.

CORS:
- One named policy; origins only from `Cors:AllowedOrigins` per environment
  (Development: `http://localhost:5173` for Vite, `http://localhost:3000` for Next.js;
  none elsewhere until the frontend domain is known). Wildcards, paths and trailing
  slashes stop startup.
- Methods GET, POST, PUT, DELETE; headers Authorization and Content-Type; exposed
  headers Retry-After (needed for the 429 message) and Location. No credentials:
  tokens travel in the Authorization header, not in cookies.
- UseCors runs before authentication, so error responses (400, 401, 403, 429, 5xx)
  are readable by the browser too.

Product writes:
- POST, PUT and DELETE /api/products use the existing LocalAdmin policy (Role=Admin in
  the SQL profile, read on every request). No token returns 401, a regular user 403.
  GET routes stay public. tools/Pikwise.DataImport writes through the database and is
  unaffected.
- Roles are still changed only by the database owner in SQL; no role-changing API is
  added (it would be a privilege-escalation surface).
