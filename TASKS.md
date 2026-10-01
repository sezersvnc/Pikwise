# TASKS.md — Pikwise Current Work Queue

## Current roadmap position

```text
Stage 3 — EF Core relationships     🟡 continue inside project
Stage 4 — MD architecture           ✅ done
Stage 5 — Project skeleton          ✅ complete
Stage 6 — Product system            NEXT
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
- [ ] Brand
- [ ] Category
- [ ] Product
- [ ] LaptopSpecification

Relationships:
- [ ] Brand 1 -> many Products
- [ ] Category 1 -> many Products
- [ ] Product 1 -> 1 LaptopSpecification

Practice:
- [ ] Foreign keys
- [ ] Navigation properties
- [ ] Fluent API
- [ ] Unique constraints
- [ ] Initial migration
- [ ] Inspect generated schema
- [ ] `Include`
- [ ] `ThenInclude` when needed

Exit check:
- [ ] Explain why each FK is on that entity
- [ ] Explain One-to-One vs One-to-Many without notes

---

# Session 4 — First Vertical Feature

Implement:

```text
GET /api/products/{id}
```

Required:
- [ ] ProductResponseDto
- [ ] Product mapper
- [ ] IProductRepository
- [ ] ProductRepository
- [ ] IProductService
- [ ] ProductService
- [ ] ProductsController
- [ ] DI registrations
- [ ] async EF Core query
- [ ] 404 behavior
- [ ] Swagger/Postman test

Review:
- [ ] Controller contains HTTP concerns only
- [ ] Repository contains EF Core query
- [ ] Service orchestrates the use case
- [ ] Entity is not blindly returned
- [ ] mapping location is intentional

---

# Session 5 — Complete Product CRUD

- [ ] GET all
- [ ] POST
- [ ] PUT
- [ ] DELETE
- [ ] CreateProductRequestDto
- [ ] UpdateProductRequestDto
- [ ] ProductResponseDto
- [ ] validation
- [ ] centralized exception handling baseline

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
