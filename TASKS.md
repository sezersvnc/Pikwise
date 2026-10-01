# TASKS.md — Pikwise Current Work Queue

## Current roadmap position

```text
Stage 3 — EF Core relationships     🟡 continue inside project
Stage 4 — MD architecture           ✅ done
Stage 5 — Project skeleton          ✅ complete
Stage 6 — Product system            ✅ CRUD complete
```

---

# Session 1 — Project Skeleton

Create:

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

Tasks:
- [x] Create `Pikwise.sln`
- [x] Create four source projects
- [x] Create test projects
- [x] Add project references
- [x] Confirm dependency direction from `docs/ARCHITECTURE.md`
- [x] Build the solution
- [x] Add a simple health/test endpoint
- [x] Make the first clean commit

Do not build Recommendation Engine or AI yet.

---

# Session 2 — SQL Server + EF Core

- [x] Add EF Core packages
- [x] Add SQL Server provider
- [x] Create `ApplicationDbContext`
- [x] Configure connection safely
- [x] Register DbContext with DI
- [x] Verify migration commands
- [x] Never commit secrets

---

# Session 3 — First Real Data Model

Create:
- [x] Brand
- [x] Category
- [x] Product
- [x] LaptopSpecification

Relationships:
- [x] Brand 1 -> many Products
- [x] Category 1 -> many Products
- [x] Product 1 -> 1 LaptopSpecification

Practice:
- [x] Foreign keys
- [x] Navigation properties
- [x] Fluent API
- [x] Unique constraints
- [x] Initial migration
- [x] Inspect generated schema
- [x] `Include`
- [x] `ThenInclude` when needed

Exit check:
- [x] Explain why each FK is on that entity
- [x] Explain One-to-One vs One-to-Many without notes

---

# Session 4 — First Vertical Feature

Implement:

```text
GET /api/products/{id}
```

Required:
- [x] ProductResponseDto
- [x] Product mapper
- [x] IProductRepository
- [x] ProductRepository
- [x] IProductService
- [x] ProductService
- [x] ProductsController
- [x] DI registrations
- [x] async EF Core query
- [x] 404 behavior
- [x] Swagger/Postman test

Review:
- [x] Controller contains HTTP concerns only
- [x] Repository contains EF Core query
- [x] Service orchestrates the use case
- [x] Entity is not blindly returned
- [x] mapping location is intentional

---

# Session 5 — Complete Product CRUD

- [x] GET all
- [x] POST
- [x] PUT
- [x] DELETE
- [x] CreateProductRequestDto
- [x] UpdateProductRequestDto
- [x] ProductResponseDto
- [x] validation
- [x] centralized exception handling baseline

---

# Session 6 — Complete Relationship Practice

Create:
- [ ] UserProfile
- [ ] Favorite

Relationship:

```text
UserProfile 1 -> many Favorite <- many-to-1 Product
```

Practice:
- [ ] explicit join entity
- [ ] composite key
- [ ] prevent duplicate favorite
- [ ] Include
- [ ] ThenInclude

Auth integration can follow once the core product/data model is stable.

---

# After Product CRUD

Follow `docs/ROADMAP.md`:

1. Filtering / Sorting / Pagination
2. Product Comparison
3. Recommendation Engine V1
4. Value-for-Money
5. LLM fundamentals
6. AI explanation
7. Frontend
8. Store / offers
9. Price history / background jobs
10. Alerts / periodic advisor

## Codex rule
Do not implement the whole roadmap at once. Finish and review one vertical slice before moving to the next.

## SQL Server package note

For the Infrastructure project, Codex should use:

```text
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Design
```

Do not add the PostgreSQL/Npgsql provider.
