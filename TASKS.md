# TASKS.md — Pikwise Current Roadmap

Completed:
- [x] Session 8 — Favorites API
- [x] Session 9 — Filtering, Sorting and Pagination
- [x] Session 10 — Product Comparison

Current focus:
- [ ] Session 11 — Recommendation Engine Design
- [ ] Session 11.5 — Open Dataset Bootstrap

This file continues from Recommendation Engine onward.

---

# Session 11 — Recommendation Engine Design

## Goal
Design the recommendation algorithm before implementing it.

> Do not ask Codex to invent the scoring formula.

## Decisions To Make
- [ ] Define `UserRequirements`.
- [ ] Define hard constraints.
- [ ] Define weighted-score properties.
- [ ] Define score range.
- [ ] Define normalization rules.
- [ ] Define missing-data behavior.
- [ ] Define default weights.
- [ ] Define how user importance affects weights.
- [ ] Define score-component output.
- [ ] Define deterministic tie-breaking.
- [ ] Verify sample rankings manually.
- [ ] Update `docs/RECOMMENDATION_ENGINE.md`.
- [ ] Record important decisions in `docs/DECISIONS.md`.

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

- [ ] Verify the dataset license.
- [ ] Verify whether commercial use is allowed.
- [ ] Record the source and license in project documentation.
- [ ] Do not treat a random public CSV as automatically open/licensed data.
- [ ] Avoid importing restricted or unclear data into the real product.

## Tasks

- [ ] Select a laptop/e-commerce dataset with clear usage terms.
- [ ] Save source URL/name/license information in documentation.
- [ ] Define `LaptopImportDto` or equivalent import model.
- [ ] Build an import service.
- [ ] Map external field names to Pikwise fields.
- [ ] Normalize RAM values.
- [ ] Normalize storage values.
- [ ] Normalize CPU names.
- [ ] Normalize GPU names.
- [ ] Normalize price values.
- [ ] Handle missing fields explicitly.
- [ ] Prevent duplicate imports where practical.
- [ ] Import an initial realistic sample into SQL Server.
- [ ] Validate imported products through existing Product endpoints.
- [ ] Add tests for normalization/import behavior.
- [ ] Update `docs/DATABASE.md` if import assumptions affect the data model.
- [ ] Add a short dataset/source note to project documentation.

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
- [ ] Implement hard filters.
- [ ] Implement normalization.
- [ ] Implement weighted scoring.
- [ ] Preserve score components.
- [ ] Rank eligible products.
- [ ] Return Top 3.
- [ ] Add deterministic tie-breaking.
- [ ] Add unit tests.
- [ ] Add boundary tests.
- [ ] Verify same input produces same output.
- [ ] Keep the LLM completely outside the scoring decision.
- [ ] Build and run all tests.

## Exit Criterion
The same input and database state always produce the same ranking, with numerical score explanations.

---

# Session 13 — Value-for-Money

## Goal
Distinguish best fit from best value.

## Tasks
- [ ] Define price-vs-score gain.
- [ ] Detect expensive upgrades with small suitability gain.
- [ ] Detect cheaper near-equal alternatives.
- [ ] Return value analysis as structured data.
- [ ] Add unit tests.
- [ ] Update recommendation documentation.

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
- [ ] Define JSON Schema / structured output.
- [ ] Create an LLM service abstraction.
- [ ] Keep provider-specific details outside core business logic.
- [ ] Convert natural language to `UserRequirements`.
- [ ] Validate LLM output before sending it to the Recommendation Engine.
- [ ] Add timeout/error handling.
- [ ] Never let the LLM bypass hard constraints.

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
- [ ] Build explanation input DTO.
- [ ] Add explanation service.
- [ ] Prevent unsupported product facts.
- [ ] Handle missing information honestly.
- [ ] Ensure ranking exists before the LLM call.
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

# Session 16 — Frontend Foundation

## Goal
Connect the existing ASP.NET Core backend to a real web interface.

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
