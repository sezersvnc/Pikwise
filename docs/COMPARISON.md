# Session 10 — Product comparison

## Contract

```http
GET /api/products/compare?ids=1&ids=2
GET /api/products/compare?ids=3&ids=1&ids=2
```

Use exactly 2 or 3 distinct positive Int32 IDs. Repeated query parameters preserve
the user's selection order. Duplicates are rejected rather than silently removed.
IDs must be supplied individually; comma-separated values are not supported.

| Result | HTTP behavior |
|---|---|
| All selected products exist | 200 with products in request order |
| Invalid count, duplicate, nonpositive or malformed ID | 400 ValidationProblemDetails |
| At least one selected product is absent | 404 ProblemDetails with missingProductIds; no partial result |

The response is an object with a products array. Each item exposes id, name, price,
brand { id, name } and specification. The existing LaptopSpecificationDto supplies
processor, gpu, ramGb, storageGb, screenSize, resolution, refreshRate, weight and
operatingSystem. RAM/storage use GB, screenSize inches and weight kg. Reusing the
contract and mapper keeps facts consistent with product detail and catalog output.

Missing specifications remain null, as permitted by the persistence model. The
endpoint does not invent facts or reject a product solely for incomplete specs.
Inactive/out-of-stock products remain comparable. No category, stock, timestamps,
favorite/profile data, scores, winners or explanations are returned. Catalog
reads remain public. Product-write authorization is a separate future decision.

## Request flow and review

1. ProductsController binds ProductComparisonRequestDto and owns HTTP metadata.
   The literal compare route coexists with the existing {id:int} route.
2. ProductService.CompareAsync validates selections through ProductComparisonValidator,
   calls GetByIdsAsync once and identifies missing IDs. It raises an Application
   ProductsNotFoundException if any are missing; API maps it to 404.
3. Infrastructure's ProductRepository performs a parameterized membership query
   with AsNoTracking and Include for Brand and LaptopSpecification. No per-ID
   database calls, Category loading or favorite/profile loading are required.
4. Service restores request order because SQL set results have no guaranteed
   selection order. ProductMapper.ToComparisonDto maps each selected entity.
5. ASP.NET Core serializes the explicit response DTO; entities are never returned.

Review Application/Products/DTOs/ProductComparisonRequestDto.cs,
ProductComparisonResponseDto.cs, ProductComparisonItemDto.cs, Validators/
ProductComparisonValidator.cs, Exceptions/ProductsNotFoundException.cs,
Services/ProductService.cs, Mappers/ProductMapper.cs, Infrastructure/Products/
ProductRepository.cs, Api/Controllers/ProductsController.cs and Errors/ApiExceptionHandler.cs.

Annotation and cross-field rules also run in Service for non-HTTP callers.
CancellationToken is passed to the awaited repository query. Validation, selection
completeness and order belong to Application; EF and joins stay in Infrastructure.
No new service abstraction is needed for this small product use case. No Domain,
schema, migration, package or authentication change is required.

The query reflects stored facts when read. It does not lock products against
later edits/deletion or provide a lasting snapshot across separate requests.
Recommendation design remains Session 11; this endpoint adds no scoring or LLM.

## Verification

Release build: zero warnings/errors. All 93 tests pass: 18 unit and 75 integration.
Eight SQL tests use only the guarded PikwiseSession3Tests database; 85 tests run
without SQL Server.

- ProductComparisonTests verifies request order, every mapped specification field,
  missing-spec null, cancellation, one repository call, missing ID order and
  rejection before data access for invalid non-HTTP selections.
- ProductComparisonValidationTests exercises the real HTTP pipeline for missing,
  insufficient, excessive, duplicate, nonpositive, malformed and overflowing IDs.
- ProductComparisonSqlTests checks 200 for two and three products, all mapped facts,
  null specs, selected-only results, safe JSON fields and 404 without partial output.
  An interceptor confirms one SQL query and no tracked entities. Unique fixture
  rows are removed from the dedicated test database after use.
- OpenApiTests verifies the compare route, query parameter and 200/400/404 contracts.
  Existing CRUD, filtering, favorites and authentication tests continue to pass.

[Pikwise.http](Pikwise.http) supplies local requests. Set comparison IDs to existing
products. Live checks and pending developer review are recorded in TASKS.md.
