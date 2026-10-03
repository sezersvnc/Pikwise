# TASKS.md — Pikwise Current Work Queue

## Current Status

Completed:
- [x] Project skeleton
- [x] SQL Server + EF Core setup
- [x] Core entity model and relationships
- [x] Product CRUD API
- [x] UserProfile and Favorite tables
- [x] Session 6 tests: 18 passed, 0 build errors, 0 warnings
- [x] Completed Session 6 work pushed to GitHub
- [x] Authentication foundation / local authorization test policy (Session 7)
- [x] Favorites API (Session 8)
- [x] Filtering / Sorting / Pagination (Session 9)
- [x] Product Comparison (Session 10)

Not implemented yet:
- [ ] Product-write authorization / full role management
- [ ] Recommendation Engine
- [ ] Value-for-Money
- [ ] LLM integration

> Rule: Complete, review, build, and test each session before starting the next one.

---

# Session 7 — Authentication Foundation

## Goal
Add authentication without creating a custom password/login system.

Preferred direction:
- Supabase Auth issues access tokens.
- ASP.NET Core validates Bearer JWTs.
- Pikwise owns authorization and business rules.
- Local `UserProfile` is matched from the authenticated user's `sub` claim.
- Never store passwords in Pikwise.

## Tasks
- [x] Review existing auth packages/configuration.
- [x] Add JWT Bearer authentication.
- [x] Read Supabase JWT settings from configuration/environment.
- [x] Configure `UseAuthentication()` and `UseAuthorization()` in the correct order.
- [x] Add a protected test endpoint.
- [x] Use `[Authorize]`.
- [x] Read the authenticated user's `sub` claim.
- [x] Map the provider user to local `UserProfile`.
- [x] Do not accept UserProfileId from the client for user-owned operations.
- [x] Add/update tests.
- [x] Build the solution and run all tests.
- [x] Update relevant docs if implementation differs from current decisions.

## Required behavior
```text
Valid token        -> authenticated request succeeds
No token           -> 401 Unauthorized
Invalid/expired    -> 401 Unauthorized
Authenticated but forbidden -> 403 Forbidden
```

## Exit Criterion
A protected endpoint identifies the authenticated user from claims and unauthenticated requests correctly return 401.

Implemented: `/api/auth/me` and `/api/auth/admin-check`. Release build:
0 errors/warnings; 44 tests passed. Real Supabase JWKS checked and local HTTP
401 behavior verified. On 2026-10-03, a real Supabase user access token was used
successfully for GET /api/auth/me in Postman, and the user confirmed the SQL
profile with Role=User. Real-token Favorites/Admin-check manual tests remain pending.
See `docs/AUTHENTICATION.md`. Favorites API is implemented in Session 8.

---

# Session 8 — Favorites API

## Goal
Use the existing `UserProfile -> Favorite <- Product` relationship in a real authenticated feature.

## Suggested Endpoints
```text
GET    /api/favorites
POST   /api/favorites/{productId}
DELETE /api/favorites/{productId}
```

## Tasks
- [x] Add Favorite repository abstraction/implementation if consistent with current architecture.
- [x] Add Favorite service.
- [x] Add Favorites controller.
- [x] Resolve current user from authentication claims.
- [x] Add a product to current user's favorites.
- [x] Remove a product from current user's favorites.
- [x] Return current user's favorites.
- [x] Prevent duplicate favorites.
- [x] Handle nonexistent products.
- [x] Do not receive `UserProfileId` from route/body.
- [x] Use DTOs.
- [x] Use async EF Core operations.
- [x] Pass `CancellationToken`.
- [x] Add tests.
- [x] Build and run all tests.

## Exit Criterion
An authenticated user can add, list, and remove only their own favorites.

Implemented: GET/POST/DELETE favorites, with 201/204 success, duplicate
409 and owner-filtered 404 behavior. All 57 tests pass; Release build has zero
errors/warnings. See `docs/FAVORITES.md`. Session 9 is recorded below.

---

# Session 9 — Filtering, Sorting and Pagination

## Goal
Turn Product CRUD into a realistic catalog API.

## Example
```http
GET /api/products?minPrice=20000&maxPrice=50000&brandId=2&minRam=16&sortBy=price&page=1&pageSize=20
```

## Topics
- `IQueryable`
- deferred execution
- `Where`
- `OrderBy`
- `Skip`
- `Take`
- query parameters
- database-side filtering
- pagination metadata

## Tasks
- [x] Create product query/filter request model.
- [x] Add min/max price filtering.
- [x] Add brand filtering.
- [x] Add RAM filtering.
- [x] Add selected CPU/GPU/spec filters when useful.
- [x] Add sorting.
- [x] Add pagination.
- [x] Define safe max `pageSize`.
- [x] Return pagination metadata.
- [x] Keep query composable with `IQueryable`.
- [x] Avoid `ToListAsync()` before filters/pagination.
- [x] Add combined-filter tests.
- [x] Build and run all tests.

## Exit Criterion
Combined product queries work predictably through Swagger/Postman.

## Verification and review status

Implemented and verified on 2026-10-03: combined price/brand/RAM/storage/CPU/GPU
queries, explicit sorting with ID ties, max pageSize 100 and paged metadata.
Release build: zero errors/warnings; all 74 tests pass (9 unit, 65 integration),
including 7 guarded SQL Server tests. Live localhost:5080 checks returned 200
for valid/empty queries and 400 for invalid sizes/ranges. OpenAPI exposes all
query parameters and the paged response. The local catalog is currently empty;
populated filter/sort/page behavior was verified by HTTP tests against dedicated
SQL fixtures. Swagger/Postman developer review remains pending; examples are in
docs/Pikwise.http. See docs/PRODUCT_QUERIES.md. User approved commit/push;
Session 9 was committed as 5b950a5 and pushed to main. Session 10 is recorded below.

---

# Session 10 — Product Comparison

## Goal
Compare 2–3 laptops using verified structured data.

## Suggested Endpoint
```http
GET /api/products/compare?ids=1&ids=2&ids=3
```

## Comparison Fields
- Price
- Brand
- Processor
- GPU
- RAM
- Storage
- Screen size
- Resolution
- Refresh rate
- Weight
- Operating system

## Tasks
- [x] Define comparison request/response DTO.
- [x] Support multiple product IDs.
- [x] Validate comparison count.
- [x] Load required product/spec data efficiently.
- [x] Return comparable fields in one response.
- [x] Handle nonexistent IDs.
- [x] Do not use an LLM.
- [x] Do not calculate recommendation scores yet.
- [x] Add tests.
- [x] Build and run all tests.

## Exit Criterion
Two or three laptops can be compared in one structured response.

## Verification and review status

Implemented and verified on 2026-10-03: public GET /api/products/compare accepts
2-3 distinct positive IDs, returns stored comparison facts in selection order,
preserves missing-spec null and reports all missing IDs with 404. One SQL query
loads the selection. All 93 tests pass (18 unit, 75 integration), including 8
guarded SQL Server tests. Release build: zero errors/warnings.

Live localhost:5080 checks returned 400 for missing/count/duplicate errors,
404 with missingProductIds for absent products, and 200 for the unchanged catalog
list. OpenAPI exposes the comparison query and 200/400/404 responses. The local
catalog is empty; successful two/three-product HTTP comparisons were verified
against populated fixtures in the dedicated SQL test database. Swagger/Postman
developer review remains pending; examples are in docs/Pikwise.http. See
docs/COMPARISON.md. User approved Session 10 commit/push. Session 11 has not started.

---

# Session 11 — Recommendation Engine Design

## Goal
Design the algorithm before implementing it.

> Do not ask Codex to invent the scoring formula.

## Decisions
- [ ] Define `UserRequirements`.
- [ ] Define hard constraints.
- [ ] Define weighted-score properties.
- [ ] Define score range.
- [ ] Define normalization rules.
- [ ] Define missing-data behavior.
- [ ] Define default weights.
- [ ] Define score-component output.
- [ ] Define deterministic tie-breaking.
- [ ] Verify sample rankings manually.
- [ ] Update `docs/RECOMMENDATION_ENGINE.md`.
- [ ] Record important choices in `docs/DECISIONS.md`.

## Target Flow
```text
User Requirements
        ↓
Hard Filters
        ↓
Normalization
        ↓
Weighted Scoring
        ↓
Ranking
        ↓
Top 3
```

## Exit Criterion
We can manually explain why Product A ranks above Product B for a sample user.

---

# Session 12 — Recommendation Engine V1

## Goal
Implement the deterministic engine designed in Session 11.

## Tasks
- [ ] Implement hard filters.
- [ ] Implement normalization.
- [ ] Implement weighted scoring.
- [ ] Preserve score components.
- [ ] Rank eligible products.
- [ ] Return Top 3.
- [ ] Add deterministic tie-breaking.
- [ ] Add unit tests and boundary tests.
- [ ] Verify same input produces same output.
- [ ] Keep LLM outside the scoring decision.
- [ ] Build and run all tests.

## Exit Criterion
The same input and database state always produce the same ranking, with numerical score explanations.

---

# Session 13 — Value-for-Money

## Goal
Distinguish best fit from best value.

## Tasks
- [ ] Define price-vs-score gain.
- [ ] Detect expensive upgrades with small gain.
- [ ] Detect cheaper near-equal alternatives.
- [ ] Return value analysis as structured data.
- [ ] Add unit tests.
- [ ] Update docs.

## Example
```text
Laptop A -> 90 points -> 40,000 TL
Laptop B -> 92 points -> 48,000 TL

Extra 8,000 TL -> only +2 suitability points
```

## Exit Criterion
The API can show whether paying more produces meaningful benefit.

---

# Session 14 — LLM Structured Input

## Goal
Use an LLM only to convert natural-language needs into structured recommendation criteria.

## Tasks
- [ ] Define JSON Schema / structured output.
- [ ] Create LLM service abstraction.
- [ ] Keep provider details outside core business logic.
- [ ] Convert natural language to `UserRequirements`.
- [ ] Validate LLM output before Recommendation Engine.
- [ ] Add timeout/error handling.
- [ ] Never let LLM bypass hard constraints.

## Exit Criterion
Natural-language input becomes validated structured criteria.

---

# Session 15 — AI Explanation

## Goal
Explain deterministic Recommendation Engine results.

## LLM Input
Only:
- UserNeeds
- verified ProductFacts
- EngineScore
- score components
- value-for-money analysis

## Tasks
- [ ] Build explanation input DTO.
- [ ] Add explanation service.
- [ ] Prevent unsupported facts.
- [ ] Handle missing information honestly.
- [ ] Ensure ranking exists before LLM call.
- [ ] Add tests where practical.

## Architectural Rule
```text
Product DB            = facts
Recommendation Engine = decision
LLM                   = understanding + explanation
```

## Exit Criterion
AI can explain "Why A?" or "Is B worth 5,000 TL more?" using only verified data.

---

# Later Roadmap — Do Not Start Yet

- [ ] Frontend
- [ ] Store / ProductOffer system
- [ ] Permitted product-data integrations
- [ ] PriceHistory
- [ ] Background jobs
- [ ] Price alerts
- [ ] Periodic advisor
- [ ] Additional product categories

Do not add yet:
- microservices
- Kafka/event bus
- Redis without measured need
- browser extension
- mobile app
- all categories at once

---

# Session Completion Rule

At the end of every session:
1. Build the complete solution.
2. Run all tests.
3. Test affected endpoints through Swagger/Postman.
4. Review layer responsibilities.
5. Review EF Core queries and relationships.
6. Check build warnings.
7. Check no secret is committed.
8. Update `TASKS.md`.
9. Update relevant files under `docs/`.
10. Commit/push only after review.

## Learning Rule
Do not move on just because Codex finished coding.

Before marking a session complete, be able to answer:
- What was added?
- Why does it belong in that layer?
- What request flow does it follow?
- What database query is produced conceptually?
- What could go wrong?
- Which tests prove it works?
