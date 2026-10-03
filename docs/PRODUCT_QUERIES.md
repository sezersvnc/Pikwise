# Session 9 — Product catalog queries

## Request flow and code review

1. ProductsController binds ProductQueryRequestDto from query parameters and owns
   HTTP 200/400. ASP.NET Core handles parsing and serialization.
2. ProductService validates the request for non-HTTP callers too, calls the
   repository, maps the returned entities and attaches pagination metadata.
3. ProductRepository composes IQueryable with Where, selects explicit sorting
   expressions, counts matches and executes Skip/Take before ToListAsync.
4. EF Core translates the expressions into parameterized SQL Server queries.

Review Application/Products/DTOs/ProductQueryRequestDto.cs, Validators/
ProductQueryValidator.cs, Services/ProductService.cs, Models/ProductPageResult.cs,
Infrastructure/Products/ProductRepository.cs and Api/Controllers/ProductsController.cs.
IQueryable stays inside Infrastructure. DTOs expose no EF entities or user data.
The existing mapper handles C# conversion for only the returned page.

## Contract decisions

The product list now returns an object with items and page metadata instead of
an array. Defaults are page 1, pageSize 20 and ID ascending; max pageSize is 100.
The maximum SQL offset is Int32.MaxValue. Invalid values are rejected rather than
silently clamped. Optional decimal filters accept null; DecimalScaleAttribute
now leaves missing values to Required where applicable.

All filters intersect. Price bounds are inclusive and use the stored price's
precision. Brand matching uses ID. RAM/storage use minimum GB. CPU/GPU are literal
substring searches after trimming; spaces alone disable those filters. SQL Server
collation determines text matching and name ordering. Missing specifications are
excluded only when a specification filter is supplied. RAM sorting uses SQL NULL
ordering: missing specifications precede values ascending and follow descending.

Sort fields are id, price, name, ram and createdAt, with asc/desc direction.
Keywords use ordinal case-insensitive comparison, so Turkish casing does not
reject PRICE. Explicit LINQ expressions avoid accepting arbitrary SQL fields.
Ascending ID breaks equal sort values. A unique order is required for dependable
paging on unchanged data; see [EF Core pagination](https://learn.microsoft.com/en-us/ef/core/querying/pagination).

Inactive products and products without stock remain visible, preserving existing
catalog behavior. Favorites still return their existing array. No schema or
migration changes are required.

## SQL behavior and limits

Conceptually the repository executes:

```sql
SELECT COUNT(*) FROM Products LEFT JOIN LaptopSpecifications ... WHERE ...;
SELECT ... FROM Products LEFT JOIN LaptopSpecifications ...
WHERE ... ORDER BY Price, Id OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
```

Brand/category/specification relationships are loaded for response mapping.
Count and page queries run sequentially on one context and await cancellation.
They use separate statements without a snapshot transaction: concurrent writes
can change the count or move rows between requests. Offset cost grows for deep
pages; substring searches can scan. The page-size limit bounds returned rows,
not database work. Future measured performance needs can justify indexes/keyset
paging. No speculative indexes or extra infrastructure are added here.

## Verification

At Session 9 completion, the Release build had zero warnings/errors and all 74 tests passed: 9 unit and 65 integration,
including 7 tests on the dedicated PikwiseSession3Tests SQL Server database.
67 tests need no SQL Server.

ProductQuerySqlTests exercises individual and combined filters, every sorting
field/direction, price ties, trimming, missing specifications, default page size,
last/beyond-last/empty pages and unknown brands. Its interceptor verifies two SQL
commands, a filtered COUNT and server OFFSET/FETCH pagination. Fixtures use unique
lookup rows and clean up only their own data. Validation HTTP tests verify 400
before SQL access; unit tests check service mapping, totals and cancellation.
The existing CRUD test and OpenAPI contract test were updated.

Local request examples are in [Pikwise.http](Pikwise.http). The automated SQL tests
exercise the real HTTP pipeline; live endpoint checks are recorded in TASKS.md.
Swagger/Postman review remains a developer review step. Session 9 was approved
for commit/push; Session 10 comparison is documented in [COMPARISON.md](COMPARISON.md).
