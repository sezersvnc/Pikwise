# TASKS.md — Pikwise Current Roadmap

Completed:
- [x] Session 8 — Favorites API
- [x] Session 9 — Filtering, Sorting and Pagination
- [x] Session 10 — Product Comparison

Current focus:
- [x] Session 11 — Recommendation Engine Design
- [x] Session 11.5 — Open Dataset Bootstrap
- [x] Session 12 — Recommendation Engine V1
- [x] Session 13 — Value-for-Money
- [x] Session 14 — LLM Structured Input (no real provider yet)
- [x] Session 15 — AI Explanation
- [x] Session 15.5 — LLM provider (Groq)

This file continues from Recommendation Engine onward.

---

# Session 11 — Recommendation Engine Design

## Goal
Design the recommendation algorithm before implementing it.

> Do not ask Codex to invent the scoring formula.

## Decisions To Make
Status: complete. Decisions are recorded in `docs/RECOMMENDATION_ENGINE.md`, ADR-019 and ADR-021.
- [x] Define `UserRequirements`. (all hard constraints optional; importance 1..5, default 3)
- [x] Define hard constraints. (budget, min RAM, min storage, max weight; strict; unknown field removes the product; IsActive/Stock eligibility)
- [x] Define weighted-score properties. (RAM, storage, CPU tier, GPU tier, weight, refresh rate; price only a hard filter)
- [x] Define score range. (components 0..1, shown 0..100)
- [x] Define normalization rules. (fixed clamped ranges: RAM 8-32, storage 256-1024, weight 1.0-2.5 inverted, refresh 60-165; CPU/GPU five-tier table)
- [x] Define missing-data behavior. (exclude unknown criteria, rescale weights, report unknowns; minimum known share 50%)
- [x] Define default weights. (equal: every criterion defaults to importance level 3)
- [x] Define how user importance affects weights. (levels 1..5, weight = level / sum of levels)
- [x] Define score-component output.
- [x] Define deterministic tie-breaking. (decimal, rounded to 2 decimals; then price ascending, then Id ascending)
- [x] Verify sample rankings manually. (real 25-laptop ranking in RECOMMENDATION_ENGINE.md)
- [x] Update `docs/RECOMMENDATION_ENGINE.md`.
- [x] Record important decisions in `docs/DECISIONS.md`.

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


# Session 11.5 — Open Dataset Bootstrap

## Goal
Load a realistic laptop dataset into Pikwise before implementing Recommendation Engine V1, so scoring is tested against more than a handful of manually entered products.

This session is for **development/bootstrap data**, not yet for production store integrations.

## Core Rule

```text
Open Dataset
↓
Import DTO
↓
Normalizer
↓
Pikwise Product + LaptopSpecification
↓
SQL Server
```

The external dataset schema must not leak directly into Pikwise Domain entities.

## Dataset Requirements

Prefer a dataset that includes as many of these fields as possible:

- Brand
- Model / Product name
- CPU
- GPU
- RAM
- Storage
- Screen size
- Resolution
- Refresh rate
- Weight
- Operating system
- Price

## Licensing Rule

Before importing a dataset:

- [x] Verify the dataset license. (Open Icecat, Open Content License v1.4; see DATABASE.md / ADR-020)
- [x] Verify whether commercial use is allowed. (accepted for dev/test only; LLM use not approved; Session 15 needs permission or another source)
- [x] Record the source and license in project documentation.
- [x] Do not treat a random public CSV as automatically open/licensed data.
- [x] Avoid importing restricted or unclear data into the real product.

## Tasks

- [x] Select a laptop/e-commerce dataset with clear usage terms. (Open Icecat)
- [x] Save source URL/name/license information in documentation.
- [x] Define `LaptopImportDto` or equivalent import model. (`ExternalLaptopRecord`)
- [x] Build an import service. (`LaptopImportPreparer`, `LaptopImportService`, `tools/Pikwise.DataImport`)
- [x] Map external field names to Pikwise fields. (Icecat names verified with `inspect` on live data)
- [x] Normalize RAM values. (GB; TB x1000; sub-GB values dropped and reported)
- [x] Normalize storage values. (GB; "1 TB" -> 1000)
- [x] Normalize CPU names. (manufacturer + family + model, e.g. "Intel Core Ultra 7 155H")
- [x] Normalize GPU names. (discrete model preferred; placeholders and "UMA" treated as missing)
- [x] Normalize price values. (development CSV, invariant culture, 2 decimals; not market data)
- [x] Handle missing fields explicitly. (null = unknown; nothing guessed)
- [x] Prevent duplicate imports where practical. (unique (Provider, ExternalId); second run: 25 AlreadyImported)
- [x] Import an initial realistic sample into SQL Server. (25 laptops, 10 brands, into PikwiseDb)
- [x] Validate imported products through existing Product endpoints. (Postman: list, RTX filter + price sort, min RAM, null refresh rate, compare; all 200)
- [x] Add tests for normalization/import behavior. (unit tests pass locally)
- [x] Update `docs/DATABASE.md` if import assumptions affect the data model.
- [x] Add a short dataset/source note to project documentation.

## Session 11.5 decisions (approved)
- Provider abstraction with Open Icecat first; see ADR-020. Importer is a console tool, dry run first.
- Price/Stock/IsActive from a development CSV (not market data; originally to be removed in Session 13, kept until a real price source exists per ADR-024).
- Dry-run report must be reviewed before any CPU/GPU tier table or reference range is defined.
- Migration is created but NOT applied until the user approves its SQL/schema impact. (approved and applied)
- Icecat data must not feed LLM explanations (Session 15 needs permission or another source). (Permission received 7 October 2026 for request-time explanation input only; see ADR-025.)

## Session 11.5 verification (run locally by the user)
- Build and unit tests pass; full test suite passed with the new migration on the test database. (Originally recorded as 123/123; recounted in Session 12 on the same code: 132 tests, 124 without SQL Server + 8 SqlServer.)
- Migration `Session115_ExternalReferencesAndNullableSpecs` reviewed, then applied to PikwiseDb.
- Dry run: 25 of 25 usable; import: 25 Imported; repeated import: 25 AlreadyImported.

## Example

External dataset:

```text
brand = Lenovo
model = Legion 5
ram = "16 GB"
storage = "512GB SSD"
gpu = "RTX 4060"
price = "49999"
```

Normalized Pikwise data:

```text
Product
- Name = Lenovo Legion 5
- Brand = Lenovo
- Price = 49999

LaptopSpecification
- RamGb = 16
- StorageGb = 512
- GPU = RTX 4060
```

## Important Boundary

This session does **not** implement:

- live store APIs,
- ProductOffer,
- Store entities,
- price history,
- background jobs,
- scraping,
- real-time stock updates.

Those belong to later external-data sessions.

## Why This Happens Before Recommendation Engine V1

The Recommendation Engine should be evaluated against realistic data volume and variation.

Instead of testing:

```text
5 manually entered laptops
```

we want something closer to:

```text
100+ normalized laptop records
```

where possible.

This makes it easier to detect:
- bad score weights,
- poor normalization,
- missing-data problems,
- unrealistic rankings,
- duplicate/model-quality issues.

## Exit Criterion

Pikwise contains a clean, normalized, legally usable development dataset large enough to meaningfully test Recommendation Engine V1.

---

# Session 12 — Recommendation Engine V1

## Goal
Implement the deterministic engine designed in Session 11.

## Tasks
- [x] Implement hard filters. (eligibility + budget/min RAM/min storage/max weight, strict, unknown field removes)
- [x] Implement normalization. (fixed clamped ranges, inverted weight, CPU/GPU tier table)
- [x] Implement weighted scoring. (importance 1..5, default 3; unknown criteria rescaled; 50% known-share minimum)
- [x] Preserve score components. (value, normalized value, weight, contribution; unknown criteria; known importance)
- [x] Rank eligible products.
- [x] Return Top 3. (`POST /api/recommendations`)
- [x] Add deterministic tie-breaking. (rounded score, then price, then Id)
- [x] Add unit tests. (engine, tier table, service/validation; real 25-laptop hand ranking incl. HP EliteBook 6 G1i 5th)
- [x] Add boundary tests. (inclusive constraints, range clamps, exactly 50%, half-up rounding, ties after rounding, request limits)
- [x] Verify same input produces same output. (input-order independence test; same result on local PikwiseDb)
- [x] Keep the LLM completely outside the scoring decision.
- [x] Build and run all tests. (build OK; 148 unit + 81 integration pass without SQL Server; the 9 `SqlServer` tests, including RecommendationSqlTests, pass on PikwiseSession3Tests (run by the user); 238 in total)

## Session 12 decisions (approved)
- Public `POST /api/recommendations`, JSON body; all fields optional.
- Eligibility and constraints applied in Application on candidates loaded by one untracked query.
- Request limits: budget 0.01..10,000,000 (2 decimals), RAM 1..256, storage 1..16384, weight 0.1..10 (2 decimals), importance 1..5.
- Components rounded to 4 decimals for display; score at full precision, then 2 decimals.
- Response includes a summary of removals per rule. See ADR-022.

## Exit Criterion
The same input and database state always produce the same ranking, with numerical score explanations.

---

# Session 13 — Value-for-Money

## Goal
Distinguish best fit from best value.

## Tasks
- [x] Define price-vs-score gain. (score gap, price difference, price per point vs the best fit)
- [x] Detect expensive upgrades with small suitability gain. (`isSmallGainUpgrade`)
- [x] Detect cheaper near-equal alternatives. (cheaper and at most 3.00 points lower, whole ranking)
- [x] Return value analysis as structured data. (`valueAnalysis` in POST /api/recommendations)
- [x] Add unit tests. (ValueAnalyzerTests, service mapping, real dataset; SQL test checks the JSON shape)
- [x] Update recommendation documentation. (RECOMMENDATION_ENGINE.md, API.md, ADR-024, DATABASE.md)
- [x] Run all tests. (build OK; 241 pass without SQL Server: 160 unit, 81 integration; the 9 SqlServer tests, including the updated RecommendationSqlTests, pass on PikwiseSession3Tests (run by the user); 250 in total)

## Session 13 decisions (approved)
- One near-equal threshold (3 points, rounded scores) plus price per point; best fit = rank 1.
- Best value = cheapest of the best fit and its alternatives; ties by score, then Id.
- Returned inside the existing recommendation response; the ranking is unchanged.
- The development price CSV layer stays until a real price source exists (ADR-024).

## Example

```text
Laptop A -> 90 points -> 40,000 TL
Laptop B -> 92 points -> 48,000 TL

Extra 8,000 TL -> only +2 suitability points
```

## Exit Criterion
The API can show whether paying more produces a meaningful benefit.

---

# Session 14 — LLM Structured Input

## Goal
Use an LLM only to convert natural-language needs into structured recommendation criteria.

## Example User Input

```text
50 bin TL bütçem var, okul ve yazılım için kullanacağım,
arada oyun oynarım, çok ağır olmasın.
```

## Tasks
- [x] Define JSON Schema / structured output. (`RequirementExtractionContract`: instructions + strict schema; a test keeps it in sync with the parser)
- [x] Create an LLM service abstraction. (`IRequirementExtractor`: user text in, raw JSON out)
- [x] Keep provider-specific details outside core business logic. (Application has no provider types; Infrastructure registers `UnconfiguredRequirementExtractor` until a provider is approved)
- [x] Convert natural language to `UserRequirements`. (returns the `RecommendationRequestDto` shape; the client sends it to POST /api/recommendations)
- [x] Validate LLM output before sending it to the Recommendation Engine. (strict JSON: unknown/duplicate fields and string numbers rejected; same range rules as a manual request; nothing clamped)
- [x] Add timeout/error handling. (15 s timeout, also for providers that ignore cancellation; 503 unavailable, 502 unusable output, 400 invalid text)
- [x] Never let the LLM bypass hard constraints. (no field can remove a constraint; a budget is rejected when the text has no digits; the LLM never ranks)
- [x] Add tests. (33 unit, 9 integration with a fake extractor; no real LLM call; build OK, 283 pass without SQL Server: 193 unit, 90 integration; the 9 SqlServer tests pass on PikwiseSession3Tests (run by the user); 292 in total)

## Session 14 decisions (approved)
- Option (a): abstraction + fake extractor only. No real provider, package or API key in this session; adding one needs the owner's approval (cost: the owner is a student).
- Endpoint: `POST /api/recommendations/criteria`, criteria only; ranking stays a separate, deterministic call.
- Anonymous for now (no cost without a provider). Authentication and rate limiting are required before a real provider is enabled (ADR-026).
- A budget is accepted only when the user's text contains digits.

## Exit Criterion
Natural-language input becomes validated structured criteria.

---

# Session 15 — AI Explanation

## Goal
Explain deterministic Recommendation Engine results in natural language.

## LLM Input
Only provide:
- UserNeeds
- verified ProductFacts
- EngineScore
- score components
- value-for-money analysis

## Tasks
- [x] Build explanation input DTO. (`ExplanationInput`: criteria, Top 3 stored facts, scores, components, valueAnalysis; normalized values and weights left out)
- [x] Add explanation service. (`ExplanationService` + `IExplanationGenerator` abstraction; `POST /api/recommendations/explanation`)
- [x] Prevent unsupported product facts. (`ExplanationFactChecker`: every product exactly once, no other product id, every number must appear in the input (Turkish/English formats, "bin", rounding allowed); any failure rejects the whole explanation with 502)
- [x] Handle missing information honestly. (null fields and unknownCriteria are sent as unknown; the instructions require "bilgi yok"; invented numbers are rejected)
- [x] Ensure ranking exists before the LLM call. (the service ranks through IRecommendationService first and returns that ranking with the explanation; empty ranking = no LLM call)
- [x] Configure the LLM provider for zero data retention / no training before sending Icecat data (ADR-025). (Session 15.5: Groq, organization-wide ZDR enabled by the owner on 8 October 2026; Groq does not train on API data)
- [x] Send Icecat data only as request-time input; no storage, embeddings or training (ADR-025). (Session 15.5: one chat completion per request; nothing stored or embedded)
- [x] Add tests where practical. (39 unit, 8 integration with fake generator and fake ranking; build OK, 330 pass without SQL Server: 232 unit, 98 integration; the 9 SqlServer tests pass on PikwiseSession3Tests (run by the user); 339 in total)

## Session 15 decisions (approved)
- Option (a) again: abstraction + fake generator; no real provider, package or API key. The owner will name the provider; then auth + rate limiting + retention check (ADR-026, ADR-025).
- Separate endpoint `POST /api/recommendations/explanation`; the recommendation endpoint stays fast, free and deterministic.
- Any fact-check failure rejects the whole explanation (502); nothing is partially shown.
- Top 3 stays (ADR-019/022). A configurable `limit` (1..5, default 3) may be a separate later step with its own ADR.

## Architectural Rule

```text
Product DB            = facts
Recommendation Engine = decision
LLM                   = understanding + explanation
```

## Exit Criterion
AI can explain "Why A?" or "Is B worth 5,000 TL more?" using only verified data.

---

# Session 15.5 — LLM provider (Groq)

## Goal
Connect the Session 14/15 abstractions to a real, free language model without cost or data-retention risk.

## Tasks
- [x] Choose a free provider that satisfies ADR-025. (Groq free plan: no card, no charges, 429 at the limit; ZDR available; no training on API data; see ADR-028)
- [x] Owner enables Zero Data Retention and creates an API key. (organization-wide ZDR enabled 8 October 2026; key stored in User Secrets as `Groq:ApiKey` by the owner; never in the repository)
- [x] Add the Groq adapters in Infrastructure. (`GroqChatClient` typed HttpClient, strict `json_schema` output, `openai/gpt-oss-120b`, reasoning effort low, reasoning excluded; `GroqRequirementExtractor`, `GroqExplanationGenerator`)
- [x] Choose the implementation from configuration. (key present -> Groq; key missing -> unconfigured, 503; tests clear the key)
- [x] Require a signed-in user for both language model endpoints. (401 without a valid Supabase user token)
- [x] Add rate limiting. (per user 5/min, total 25/min, shared by both endpoints; 429 problem details with Retry-After; recommendations are not limited)
- [x] Never log the key, the user's text or model output. (Authorization header redacted; only HTTP status codes are logged)
- [x] Add tests. (12 unit with a fake HTTP handler, 11 integration; build OK, 353 pass without SQL Server: 244 unit, 109 integration)
- [x] Real-call smoke test by the owner (8 October 2026, Supabase test user token): criteria 200 from Groq; explanation 200 for `{budgetMax: 70000, minRamGb: 16}` (ranking 19, 8, 13; all numbers passed the fact check); 401 without token; per-user limit answered 429 after 5 calls shared by both endpoints; 353 non-SQL and 9 SqlServer tests pass (362 in total).
- [x] Clarify the valueComment instructions. (the first real valueComment mentioned "a more expensive alternative", which valueAnalysis does not contain; a digit-free claim the fact check cannot catch, see ADR-027 limits)

## Session 15.5 decisions (approved)
- Groq free plan, no payment details on the account; moving to another provider later means one new Infrastructure adapter.
- Package `Microsoft.Extensions.Http` 10.0.11 added to Infrastructure (already used by tools/Pikwise.DataImport).

## Exit Criterion
Both language model endpoints work against Groq for signed-in users, within free limits, without storing data.

---

# Session 16 — Frontend Foundation

## Goal
Connect the existing ASP.NET Core backend to a real web interface.

Owned by a teammate; the backend team does not change frontend code. Product requirements, screens and API usage: [docs/FRONTEND.md](docs/FRONTEND.md).

## Recommended Tech
- React or Next.js
- TypeScript preferred
- existing ASP.NET Core API
- Supabase Auth

## Initial Pages

```text
/
├── Home
├── Products
├── Product Detail
├── Compare
├── Recommendation
├── AI Advisor
├── Favorites
└── Auth
```

## Suggested Frontend Structure

```text
frontend/
├── src/
│   ├── app/ or pages/
│   ├── components/
│   ├── features/
│   │   ├── products/
│   │   ├── comparison/
│   │   ├── recommendation/
│   │   ├── favorites/
│   │   └── auth/
│   ├── services/
│   │   └── api/
│   ├── hooks/
│   ├── types/
│   ├── utils/
│   └── config/
├── public/
└── package.json
```

## Tasks
- [ ] Create frontend project.
- [ ] Add API base URL configuration.
- [ ] Add shared API client.
- [ ] Add Product list page.
- [ ] Add Product detail page.
- [ ] Add filtering UI.
- [ ] Add sorting UI.
- [ ] Add pagination UI.
- [ ] Add loading states.
- [ ] Add error states.
- [ ] Add responsive base layout.
- [ ] Keep API contracts aligned with `docs/API.md`.

## Exit Criterion
Users can browse products from the browser using the real Pikwise API.

---

# Session 17 — Frontend Authentication

## Goal
Connect Supabase Auth to the frontend.

See [docs/FRONTEND.md](docs/FRONTEND.md) sections 3 (R2) and 6.

## Flow

```text
User
↓
Supabase Login/Register
↓
Access Token
↓
Frontend API Client
↓
Authorization: Bearer <token>
↓
Pikwise API
```

## Tasks
- [ ] Add login page.
- [ ] Add registration page.
- [ ] Add logout.
- [ ] Add session persistence.
- [ ] Add token-aware API client.
- [ ] Send Bearer token to protected endpoints.
- [ ] Add protected UI routes where needed.
- [ ] Add current-user profile view.
- [ ] Handle 401.
- [ ] Handle 403.
- [ ] Do not store sensitive secrets in frontend source.

## Exit Criterion
A user can authenticate through Supabase and access protected Pikwise API endpoints from the frontend.

---

# Session 18 — Favorites UI

## Goal
Expose the Favorites API through the frontend.

## Tasks
- [ ] Add favorite button to Product cards/detail.
- [ ] Add remove-favorite behavior.
- [ ] Add Favorites page.
- [ ] Handle unauthenticated users.
- [ ] Add loading/error states.
- [ ] Decide whether optimistic UI is useful.
- [ ] Keep current-user ownership server-side.

## Exit Criterion
Authenticated users can manage favorites from the browser.

---

# Session 19 — Product Comparison UI

## Goal
Make product comparison understandable and visual.

## Tasks
- [ ] Add "Compare" action to product cards.
- [ ] Allow 2–3 selected products.
- [ ] Add comparison page/table.
- [ ] Show:
  - price
  - CPU
  - GPU
  - RAM
  - storage
  - display
  - refresh rate
  - weight
  - operating system
- [ ] Highlight meaningful differences.
- [ ] Handle missing values cleanly.
- [ ] Prevent invalid comparison counts.

## Exit Criterion
Users can compare 2–3 laptops side by side in the UI.

---

# Session 20 — Recommendation + AI Advisor UI

## Goal
Turn Recommendation Engine + Value-for-Money + AI Explanation into a complete user experience.

## Example User Input

```text
50 bin TL bütçem var.
Yazılım geliştireceğim.
Arada oyun oynarım.
Çok ağır olmasın.
```

## Flow

```text
Natural-language need
↓
LLM Structured Input
↓
Recommendation Engine
↓
Value-for-Money
↓
Top 3
↓
AI Explanation
↓
Frontend
```

## Tasks
- [ ] Add recommendation input page.
- [ ] Add natural-language input.
- [ ] Add structured preference controls if useful.
- [ ] Show Top 3 recommendation cards.
- [ ] Show recommendation score.
- [ ] Show score breakdown.
- [ ] Show Value-for-Money analysis.
- [ ] Show AI explanation.
- [ ] Add loading state.
- [ ] Add error/fallback state.
- [ ] Add favorite and compare actions to recommendations.

## Exit Criterion
A user can describe their needs and receive Top 3 recommendations with explanations.

---

# MVP CHECKPOINT

At this point Pikwise should support:

```text
Login/Register
↓
Browse Products
↓
Filter / Sort
↓
View Product
↓
Favorite
↓
Compare
↓
Describe Need
↓
Get Top 3
↓
See Value-for-Money
↓
Read AI Explanation
```

Before continuing:
- [ ] demo the product,
- [ ] test complete user flows,
- [ ] fix UX/API problems,
- [ ] verify recommendation quality,
- [ ] review documentation.

---

# Session 21 — External Product Data Integration

## Goal
Move from manual/sample product data to approved external APIs or feeds.

## Core Rule

```text
External API model != Pikwise domain model
```

## Target Flow

```text
External API / Feed
↓
External DTO
↓
Provider Mapper
↓
Normalization
↓
Product Matching
↓
Product / ProductOffer
↓
SQL Server
```

## Tasks
- [ ] Define external provider abstraction.
- [ ] Add provider-specific DTOs.
- [ ] Add mapper/normalizer.
- [ ] Normalize brand/model names.
- [ ] Normalize CPU/GPU/spec formats.
- [ ] Normalize price/currency/stock.
- [ ] Define product identity/matching strategy.
- [ ] Prevent duplicate real-world products.
- [ ] Keep provider schema out of Domain/Application.
- [ ] Add fake-provider tests.

## Exit Criterion
An external provider response can be converted into normalized Pikwise product data.

---

# Session 22 — Store / ProductOffer

## Goal
Separate the product itself from store-specific offers.

## Model

```text
Product
  1
  |
  ∞
ProductOffer
  ∞
  |
  1
Store
```

## Example

```text
Product: Lenovo Legion 5

Store A -> 42,999 TL
Store B -> 43,500 TL
Store C -> 41,800 TL
```

## Tasks
- [ ] Add `Store`.
- [ ] Add `ProductOffer`.
- [ ] Define offer uniqueness rules.
- [ ] Store offer URL.
- [ ] Store current price.
- [ ] Store stock state.
- [ ] Link external provider data to offers.
- [ ] Keep affiliate commission outside recommendation score.
- [ ] Add offer queries and DTOs.
- [ ] Add tests.

## Exit Criterion
One Product can have multiple normalized store offers.

---

# Session 23 — Price History

## Goal
Track offer price changes over time.

## Proposed Entity

```text
PriceHistory
- Id
- ProductOfferId
- Price
- RecordedAt
```

## Tasks
- [ ] Add PriceHistory.
- [ ] Store price snapshots.
- [ ] Query 30-day history.
- [ ] Query 90-day history.
- [ ] Calculate historical low.
- [ ] Detect meaningful price change.
- [ ] Prepare chart-ready API response.
- [ ] Add tests.

## Exit Criterion
Pikwise can show how a product's price changed over time.

---

# Session 24 — Background Jobs

## Goal
Run recurring work without requiring an HTTP request.

## Jobs
- refresh offers,
- record price history,
- evaluate alerts,
- generate advisor summaries.

## Tasks
- [ ] Choose a background-job mechanism appropriate for current scale.
- [ ] Add external-offer refresh job.
- [ ] Add price-history job.
- [ ] Add alert evaluation job.
- [ ] Add idempotency rules.
- [ ] Add failure handling.
- [ ] Add logging.
- [ ] Add tests where practical.

## Exit Criterion
Pikwise can perform scheduled work automatically.

---

# Session 25 — Price / Stock Alerts

## Goal
Support user tracking commands.

## Example Commands
- "40 bine düşerse haber ver."
- "35–40 bin arasına girerse bildir."
- "Stok gelince haber ver."

## Suggested Alert Model

```text
Alert
- Id
- UserProfileId
- ProductId / ProductOfferId
- AlertType
- TargetPrice
- MinPrice
- MaxPrice
- IsActive
- CreatedAt
- TriggeredAt
```

## Tasks
- [ ] Add alert entity/model.
- [ ] Add alert CRUD.
- [ ] Add PriceBelow alert.
- [ ] Add PriceRange alert.
- [ ] Add Restock alert.
- [ ] Evaluate conditions in background jobs.
- [ ] Prevent duplicate/noisy alerts.
- [ ] Add tests.

## Exit Criterion
Users can create price/stock conditions and Pikwise can detect when they happen.

---

# Session 26 — Better Alternative Alert

## Goal
Notify users when a better purchase option appears.

## Example Commands
- "Buna yakın performansta ama daha ucuz bir ürün çıkarsa haber ver."
- "Daha iyi fiyat/performanslı alternatif çıkarsa bildir."

## Tasks
- [ ] Define "similar enough performance".
- [ ] Define minimum value improvement.
- [ ] Reuse Recommendation Engine scores.
- [ ] Compare tracked product against alternatives.
- [ ] Generate structured alert reason.
- [ ] Add tests.

## Exit Criterion
Pikwise can notify a user when a materially better alternative appears.

---

# Session 27 — Decision Change Alert

## Goal
Detect when the user's best purchasing decision changes.

## Example

```text
Last week:
Product A was the best choice.

Today:
Product B dropped in price and now gives better value.
```

## Tasks
- [ ] Store recommendation/decision snapshots where needed.
- [ ] Re-evaluate tracked decisions.
- [ ] Define meaningful-change threshold.
- [ ] Avoid tiny/noisy changes.
- [ ] Generate structured explanation.
- [ ] Add tests.

## Exit Criterion
Pikwise can say "your best option changed" and explain why.

---

# Session 28 — Periodic Personal Advisor

## Goal
Provide recurring personalized shopping summaries.

## Example Commands
- "Her cuma bana uygun en iyi 3 laptopu özetle."
- "Takip ettiğim ürünlerde bu hafta ne değişti?"
- "Bu haftaki fırsatları bana gönder."

## Summary Content
- current Top 3,
- tracked-product price changes,
- better alternatives,
- triggered alerts,
- decision changes,
- Value-for-Money changes.

## Tasks
- [ ] Add advisor subscription/preferences model.
- [ ] Define schedule/cadence.
- [ ] Generate deterministic summary data.
- [ ] Optionally let LLM turn summary data into natural language.
- [ ] Add tests.

## Exit Criterion
Users can receive recurring personalized buying summaries.

---

# Session 29 — Notification System

## Goal
Deliver alerts/advisor results to users.

## Initial Channels
- in-app notification,
- email.

Later:
- push notification.

## Tasks
- [ ] Add Notification entity.
- [ ] Add read/unread state.
- [ ] Add notification history.
- [ ] Add email delivery abstraction.
- [ ] Add retries/failure behavior.
- [ ] Separate notification creation from delivery.
- [ ] Add tests.

## Exit Criterion
Triggered alerts and advisor summaries reach users through at least one real channel.

---

# Session 30 — Personal Advisor Final Experience

## Goal
Combine all major features into one coherent product.

## End-to-End Flow

```text
User explains need
↓
LLM parses need
↓
Recommendation Engine
↓
Value-for-Money
↓
Top 3 + AI Explanation
↓
User favorites/follows product
↓
External offers are monitored
↓
Prices / stock / alternatives are evaluated
↓
Decision changes are detected
↓
Alert / periodic advisor summary
↓
Notification reaches user
```

## Exit Criterion
Pikwise behaves like a personal purchase advisor, not only a product comparison site.

---

# Later — Additional Product Categories

Do not generalize prematurely.

After the laptop flow is stable:

```text
Laptop
↓
Phone
↓
Monitor
↓
Headphone
↓
Other categories
```

When adding the second category:
- keep shared Product/Auth/Offer/Alert/Advisor infrastructure,
- introduce category-specific specification models where needed,
- refactor only patterns proven by real requirements.

---

# Session Completion Rule

At the end of every session:

1. Build the complete solution.
2. Run all tests.
3. Test affected endpoints through Swagger/Postman.
4. Test affected frontend flows in the browser.
5. Review Controller / Service / Repository / DbContext responsibilities.
6. Review frontend API/service/component separation.
7. Check EF Core queries and relationships.
8. Check build warnings.
9. Check that no secret/token is committed.
10. Update `TASKS.md`.
11. Update relevant docs.
12. Commit/push only after review.

## Learning Rule

Do not mark a session complete just because Codex finished coding.

Before moving on, be able to answer:

- What was added?
- Why does it belong in that layer?
- What request/data flow does it follow?
- What database query is produced conceptually?
- What frontend component calls which API?
- What could go wrong?
- Which tests prove it works?
