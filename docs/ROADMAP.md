# ROADMAP.md — Pikwise .NET Development Roadmap

## Main strategy
Do not wait months to finish a course.

```text
Fast review
-> architecture foundation
-> small verification
-> real project
-> Recommendation Engine
-> LLM integration
```

Learning and project development run in parallel.

---

# Stage 1 — .NET Fast Review
Topics:
- HTTP / REST
- routing
- Controller/action
- DTO / Mapper
- Repository + Interface + DI
- DbContext / migration
- CRUD
- async/await

Exit:
- explain/review a small API without relying on a tutorial for every decision.

Status: ✅ concept review completed.

---

# Stage 2 — Service Layer and Responsibility Separation
Topics:
- Controller -> Service -> Repository
- thin Controller
- data access vs business logic
- IService + repository interfaces
- DI

Exit:
- apply a business rule without placing it in Controller.

Status: ✅ core concept understood.

---

# Stage 3 — Strengthen EF Core
Topics:
- One-to-One
- One-to-Many
- Many-to-Many
- Foreign Key
- Navigation Property
- Include
- ThenInclude
- Fluent API
- indexes
- unique constraints
- migrations
- async queries

Exit:
- build/query `Brand -> Product -> LaptopSpecification` with migrations.

Status: 🟡 continue inside Pikwise.

---

# Stage 4 — Prepare MD Architecture
Files:
- PROJECT.md
- ARCHITECTURE.md
- DATABASE.md
- API.md
- RECOMMENDATION_ENGINE.md
- AI.md
- ROADMAP.md
- DECISIONS.md
- AGENTS.md
- TASKS.md

Exit:
- V0.1 direction is explicit enough for the developer and Codex.

Status: ✅ complete.

---

# Stage 5 — Project Skeleton
Target:

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

Rules:
- modular monolith
- no microservices yet

Exit:
- solution builds,
- DI works,
- health/test endpoint responds.

Status: ✅ Session 1 complete. Build and health integration test passed.

---

# Stage 6 — Product System
Entities:
- Product
- Brand
- Category
- LaptopSpecification

Implement:
- Create / Get / Update / Delete
- DTO
- Mapper
- Service
- Repository
- validation
- migrations

Data target after schema stabilizes:
- about 30–100 laptop records

Exit:
- clean CRUD via Swagger/Postman.

---

# Stage 7 — Filtering, Sorting, Pagination
Learn/use:
- LINQ
- IQueryable
- combined filters
- sorting
- pagination

Candidate filters:
- min/max price
- brand
- RAM
- CPU/GPU
- storage

Exit:
- realistic catalog query behavior.

---

# Stage 8 — Product Comparison
Implement:
- compare multiple product IDs
- common structured response
- price
- CPU
- GPU
- RAM
- storage
- weight
- display
- upgradeability if reliable

Rule:
- no LLM here.

Exit:
- compare 2–3 laptops in one response.

---

# Stage 9 — Recommendation Engine V1
Flow:

```text
User Requirements
  ->
Hard Filters
  ->
Normalization
  ->
Weighted Scoring
  ->
Value / Price Analysis
  ->
Ranking -> Top 3
```

Requirements:
- deterministic
- explainable
- preserve score components

Exit:
- same input => same result,
- answer “why 92 points?” with data.

---

# Stage 10 — Value-for-Money
Implement:
- price increase vs score gain
- meaningful upgrade detection
- cheaper near-equal alternatives

Example:
```text
+8,000 TL -> only +2 suitability points
```

Exit:
- system explains whether the extra price is justified.

---

# Stage 11 — LLM Fundamentals
Learn:
- LLM API
- system/user messages
- structured output / JSON Schema
- tool/function calling
- token/cost/error management
- hallucination boundaries
- RAG basics
- no fine-tuning unless necessary

Exit:
- natural-language need -> trustworthy JSON criteria.

---

# Stage 12 — AI Explanation
LLM receives:
- UserNeeds
- verified ProductFacts
- EngineScore

LLM must not invent:
- price
- stock
- benchmark
- specs

Exit:
- AI explains engine decisions rather than replacing them.

---

# Stage 13 — Frontend
React or Next.js:
- Home
- Products
- Product Detail
- Compare
- Recommendation
- AI Advisor

Exit:
- user can enter needs and see Top 3 recommendations with explanations.

---

# Stage 14 — Real Store / Offer System
MVP aftercare.

Entities:
- Store
- ProductOffer

Prefer:
- official APIs
- affiliate feeds
- permitted data sources

Affiliate commission must not affect recommendation score.

---

# Stage 15 — Price History + Background Jobs
MVP aftercare.

Implement:
- PriceHistory
- periodic snapshots
- 90-day graph
- historical low analysis

---

# Stage 16 — Price Alerts + Periodic Advisor
MVP aftercare.

Implement later:
- target price
- notifications
- restock summaries
- weekly reports

Browser extension/mobile remain late-stage.

---

# 8-Week Example

| Week | Focus | Working output |
|---|---|---|
| 1 | .NET review | Core concepts / small API understanding |
| 2 | Service + EF Core relationships | Layer separation + relations |
| 3 | MDs + solution skeleton | Docs + working Pikwise solution |
| 4 | Product API | Laptop CRUD + DTO + Mapper + Repository + Service |
| 5 | Filtering + Comparison | Catalog queries + compare response |
| 6 | Recommendation V1 | Hard filters + normalization + scoring |
| 7 | Value-for-Money + tests | Explainable scoring + unit tests |
| 8 | LLM integration | Natural language -> JSON -> engine -> explanation |

Current position: Week 3 / Stage 5 completed. Session 2 SQL Server + EF Core configuration and tooling are complete. Session 3 entities, relationships and InitialCreate migration are complete and verified on local SQL Server. Session 4 product endpoint has not started.

---

# Quality Gate
Quality comes from:
- clear responsibilities,
- correct dependency direction,
- business logic outside Controllers,
- correct EF Core relationships/indexes,
- deterministic Recommendation Engine,
- LLM not being source of truth,
- tests,
- validation,
- exception handling,
- logging,
- pagination/filtering,
- API contracts,
- security/secret management,
- docs kept in sync.

## Do not do yet
- early microservices
- unnecessary Redis/Kafka/event bus
- LLM choosing products
- all categories at once
- scraping + affiliate + browser extension + mobile simultaneously
- ask AI to generate the entire project in one uncontrolled step
