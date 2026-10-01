# AGENTS.md — Pikwise

## Project
Pikwise

## Product goal
Build a maintainable laptop-comparison and recommendation platform.

The first MVP focuses on laptops only. The backend must be understandable, reviewable, testable, and extensible.

## Read before coding
Before architecture-sensitive work, read:
- `docs/PROJECT.md`
- `docs/ARCHITECTURE.md`
- `docs/DATABASE.md`
- `docs/API.md`
- `docs/ROADMAP.md`
- `docs/DECISIONS.md`
- `TASKS.md`

## Core architecture rules
- Controller owns HTTP concerns.
- Service/Application owns business rules and use-case orchestration.
- Repository owns data-access operations.
- DbContext / EF Core owns persistence interaction.
- Do not put EF Core queries directly in Controllers.
- Do not put business rules in Repositories.
- Use DTOs for API request/response contracts when appropriate.
- Mapper handles C# object-to-object conversion.
- ASP.NET Core handles model binding and JSON serialization.
- Use async APIs for database/network I/O.
- Prefer simple, reviewable code over unnecessary abstractions.

## V0.1 relationship source of truth
`docs/DATABASE.md`

Initial relationships:
- `Brand 1 -> many Product`
- `Category 1 -> many Product`
- `Product 1 -> 1 LaptopSpecification`
- `UserProfile 1 -> many Favorite <- many-to-1 Product`

Favorites use an explicit join entity because relationship metadata such as `CreatedAt` is useful.

## Roadmap discipline
Follow `docs/ROADMAP.md`.

Current direction:
1. Project skeleton
2. SQL Server + EF Core
3. EF Core relationships inside the real Pikwise model
4. Product CRUD
5. Filtering / sorting / pagination
6. Product comparison
7. Recommendation Engine V1
8. Value-for-Money
9. LLM integration
10. Frontend
11. Post-MVP store/price tracking features

Do not start later stages early unless required by a direct dependency.

## AI boundaries
- AI may help with explanations, code review, boilerplate, and tests.
- AI must not silently invent architecture decisions.
- AI must not become the source of truth for product facts.
- Recommendation ranking must remain deterministic and testable.
- LLM may explain engine output; it must not independently choose the winning product.

## Do not add yet
- Microservices
- Redis without a measured need
- Kafka/event bus without a measured need
- Browser extension
- Mobile app
- Large scraping subsystem
- Multi-category support
- LLM-controlled ranking

## Security direction
Current preferred direction:
- Supabase Auth for registration/login/session/token issuance
- ASP.NET Core validates bearer tokens
- Pikwise owns authorization and business rules
- Never store plaintext passwords
- Never commit secrets

## Codex working rule
For every feature:
1. State the intended layer responsibilities.
2. Implement the smallest working vertical slice.
3. Build/test it.
4. Review whether responsibilities leaked across layers.
5. Update `TASKS.md` and affected docs.
6. Do not generate the entire system in one shot.

## Review checklist
Before considering a feature complete:
1. Is each responsibility in the correct layer?
2. Is HTTP logic separated from data access?
3. Is business logic separated from persistence?
4. Are request/response contracts explicit?
5. Are async database calls awaited correctly?
6. Are sensitive fields excluded from responses?
7. Are authorization checks applied where needed?
8. Are EF Core relationships intentional?
9. Are migrations reviewed?
10. Are docs/tasks updated?

## Database provider rule
- This project uses **SQL Server**, not PostgreSQL.
- Use `Microsoft.EntityFrameworkCore.SqlServer`.
- Do not install or configure Npgsql/PostgreSQL packages unless the architecture decision is explicitly changed.
