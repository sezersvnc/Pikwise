# API.md — Pikwise Draft Contracts

This file becomes the source of truth for public API behavior.

## Products

### GET /api/products
Status: Session 9 returns 200 with `PagedProductResponseDto`. This replaces the
Session 5 bare array: clients must read products from `items`.

| Query parameter | Behavior |
|---|---|
| minPrice / maxPrice | Inclusive, nonnegative decimal(18,2); min must not exceed max |
| brandId | Positive brand ID; an unknown brand yields an empty page |
| minRam / minStorage | Positive minimum capacity in GB, inclusive |
| cpu / gpu | Trimmed literal substring, up to 200 characters; blank means no filter |
| sortBy | id (default), price, name, ram, createdAt |
| sortDirection | asc (default), desc |
| page | Positive, default 1; offset must fit Int32 |
| pageSize | 1..100, default 20 |

Filters combine with AND. Specification filters exclude products without a
specification. Text matching/name ordering follow SQL Server collation. Sort
keywords are case-insensitive independently of server culture. Every sort uses
ascending product ID to break ties; sorting by ID itself respects sortDirection.
RAM sorting retains products without a specification (SQL NULL ordering).
Inactive/out-of-stock products remain included.

```http
GET /api/products?minPrice=20000&maxPrice=50000&brandId=2&minRam=16&sortBy=price&page=1&pageSize=20
```

Example empty response:

```json
{
  "items": [], "page": 1, "pageSize": 20, "totalCount": 0,
  "totalPages": 0, "hasPreviousPage": false, "hasNextPage": false
}
```

`totalCount` counts filtered products before pagination. Pages beyond the last
return empty items with the requested page and unchanged total. hasPreviousPage
is true when page > 1 and at least one matching page exists; hasNextPage is true
when page < totalPages. Invalid fields, sort keywords, ranges or offsets return
400 ValidationProblemDetails. See [PRODUCT_QUERIES.md](PRODUCT_QUERIES.md).

### GET /api/products/{id}
Status: implemented in Session 4.

Input:
- route parameter: `int id`

Responses:
- 200: `ProductResponseDto`
- 404: product does not exist (ASP.NET Core ProblemDetails)

`ProductResponseDto` contains the product's catalog fields, timestamps, a compact
brand object, a compact category object and an optional laptop specification.
The entity and EF Core navigation graph are not serialized directly.

Example response:

```json
{
  "id": 1,
  "name": "Example Laptop",
  "price": 51999.95,
  "stock": 8,
  "isActive": true,
  "createdAt": "2026-10-01T15:00:00+00:00",
  "updatedAt": null,
  "brand": { "id": 1, "name": "Example Brand" },
  "category": { "id": 1, "name": "Laptop" },
  "specification": {
    "processor": "Example CPU",
    "gpu": "Example GPU",
    "ramGb": 32,
    "storageGb": 1024,
    "screenSize": 16.0,
    "resolution": "2560x1600",
    "refreshRate": 165,
    "weight": 1.9,
    "operatingSystem": "Example OS"
  }
}
```

The OpenAPI document is available in Development at `/openapi/v1.json`.

### POST /api/products
Input:
- `CreateProductRequestDto`

Response:
- `ProductResponseDto`

Authorization:
- likely Admin later.

Status: implemented in Session 5. Returns 201 and the ProductResponseDto with a
Location header pointing to GET by Id. Input includes all editable catalog fields
and a required `specification` object; Id and timestamps are assigned by the server.

### PUT /api/products/{id}
Input:
- route `id`
- `UpdateProductRequestDto`

Status: implemented in Session 5. Full replacement of editable fields, including
specification. Returns 200 with the updated DTO, or 404 if the product does not exist.
CreatedAt is preserved; UpdatedAt is set by the server. Id comes from the route.

### DELETE /api/products/{id}
Input:
- route `id`

Status: implemented in Session 5. Physically deletes the product and its
specification and, since Session 6, related favorites. Returns 204; an absent
product returns 404. Lookup rows and user profiles remain.

### POST / PUT request example

BrandId and CategoryId must refer to existing lookup records. This session does
not add brand/category management endpoints or seed a product catalog.
The same requests are available in [Pikwise.http](Pikwise.http) for an HTTP client
such as the VS Code REST Client extension. Set the lookup IDs before running them.

```json
{
  "name": "Example Laptop",
  "price": 51999.95,
  "stock": 8,
  "isActive": true,
  "brandId": 1,
  "categoryId": 1,
  "specification": {
    "processor": "Example CPU",
    "gpu": "Example GPU",
    "ramGb": 32,
    "storageGb": 1024,
    "screenSize": 16,
    "resolution": "2560x1600",
    "refreshRate": 165,
    "weight": 1.9,
    "operatingSystem": "Example OS"
  }
}
```

### Validation and errors

Names must be nonblank. Since Session 11.5 every specification field is optional
(null = unknown); when supplied, text must fit the mapped string lengths and be
non-blank (blank text is stored as null). Price and stock must be nonnegative; FK IDs
and, when supplied, RAM, storage and refresh rate must be positive. ScreenSize is 0.01..999.99 inches; Weight is 0.001..999.999 kg. Price
fits decimal(18,2); price/screen allow two decimal places and weight allows three.
Names and specification strings are trimmed. The `specification` object itself is required for
create/update (its fields may be null). Missing fields with valid CLR defaults (price/stock/IsActive) use
those defaults; the JSON body itself and required strings/specification are required.

Malformed requests and invalid fields/references return 400 with
ValidationProblemDetails. Missing resources return 404. Concurrent removal or FK
conflicts during a write return 409. Unexpected failures return a generic 500
ProblemDetails with traceId; exception/SQL details are logged, not exposed.
Concurrent updates currently use last-write-wins; no row-version contract exists.

Session 7 adds protected authentication endpoints. These CRUD endpoints retain
their public contract and are intended for local development; a product-write
Admin policy remains a separate decision.

---

## Favorites (Session 8)

All routes require `[Authorize]` and resolve the current local profile from the
validated token's sub claim. No UserProfileId is accepted from route/body/query.
An unresolved profile returns 403; a missing/invalid/expired token returns 401.

### GET /api/favorites

Returns 200 with an array of FavoriteResponseDto, including [] for an empty list.
Each item has createdAt and product (the existing ProductResponseDto). Only the
caller's rows are returned, ordered by favorite creation time descending and
ProductId ascending for ties. Profile data is excluded. No pagination yet.

### POST /api/favorites/{productId}

No request body. Returns 201 with the new FavoriteResponseDto. ProductId must be
positive (400 otherwise); a missing product returns 404. A duplicate favorite
returns 409 ProblemDetails and preserves the original timestamp. Concurrent
duplicate inserts also return 409. Any existing product can be favorited,
including an inactive/out-of-stock product. Parent deletion during insertion
returns a controlled 409 write conflict.

### DELETE /api/favorites/{productId}

Returns 204 when the caller's favorite was deleted. Returns 404 if the caller has
no such favorite, including when another user has it or the product no longer
exists. Nonpositive ProductId returns 400. The product and profile remain.
See [FAVORITES.md](FAVORITES.md) for implementation, tests and review guidance.

---

## Comparison

### GET /api/products/compare (Session 10)

Public catalog read; no token required. Supply repeated `ids` query parameters:

```http
GET /api/products/compare?ids=1&ids=2&ids=3
```

Exactly 2 or 3 distinct positive Int32 IDs are required. Missing/malformed IDs,
duplicates and an invalid count return 400 ValidationProblemDetails. Comma-separated
values are not supported. If any selected ID does not exist, return 404 ProblemDetails
with `missingProductIds` in request order; no partial comparison is returned.

200 returns `ProductComparisonResponseDto` with a `products` array in the user's
selection order. Each item contains id, name, price, brand { id, name } and
specification. The specification reuses LaptopSpecificationDto: processor, gpu,
ramGb, storageGb, screenSize, resolution, refreshRate, weight and operatingSystem.
RAM/storage are GB, screenSize is inches and weight is kg. Missing specifications
are represented by null, without guessed defaults. Inactive/out-of-stock products
can be compared. Stock, timestamps, category, favorites and user data are omitted.

The endpoint returns stored facts; it computes no winner, recommendation score,
price delta or AI explanation. Required relations are loaded in one SQL query.
See [COMPARISON.md](COMPARISON.md) for review guidance and tests.

---

## Recommendation

### POST /api/recommendations
Status: Session 12. Public (no token), read-only. Runs the deterministic engine described
in [RECOMMENDATION_ENGINE.md](RECOMMENDATION_ENGINE.md); no LLM is involved.

Request body (every field optional; `{}` is valid):

| Field | Rule | Meaning |
|---|---|---|
| budgetMax | 0.01..10,000,000, at most 2 decimals | Hard constraint `price <= budgetMax` |
| minRamGb | 1..256 | Hard constraint `ramGb >= minRamGb` |
| minStorageGb | 1..16384 | Hard constraint `storageGb >= minStorageGb` |
| maxWeightKg | 0.1..10, at most 2 decimals | Hard constraint `weight <= maxWeightKg` |
| importance.ram / storage / cpu / gpu / weight / refreshRate | 1..5, default 3 | Importance level of each scored criterion |

An absent constraint is not applied. A product whose field is unknown for an active
constraint is removed. Inactive and zero-stock products are never recommended.

```http
POST /api/recommendations
Content-Type: application/json

{ "budgetMax": 70000, "minRamGb": 16,
  "importance": { "ram": 3, "storage": 3, "cpu": 4, "gpu": 2, "weight": 5, "refreshRate": 1 } }
```

200 response (shortened):

```json
{
  "items": [
    {
      "rank": 1, "productId": 13, "name": "DELL PW514265", "price": 68000.00,
      "brand": { "id": 3, "name": "DELL" },
      "specification": { "processor": "AMD Ryzen AI 7 PRO 450", "...": "..." },
      "score": 75.41, "knownImportance": 18, "totalImportance": 18, "knownWeightShare": 1,
      "unknownCriteria": [],
      "components": [
        { "criterion": "ram", "value": 32, "normalizedValue": 1, "weight": 0.1667, "contribution": 16.6667 }
      ]
    }
  ],
  "summary": { "candidateCount": 25, "removedByEligibility": 2, "removedByConstraints": 6,
               "removedByKnownShare": 0, "rankedCount": 17 },
  "valueAnalysis": { "bestFitProductId": 13, "bestValueProductId": 13, "isSmallGainUpgrade": false,
                     "nearEqualScoreGap": 3, "cheaperAlternatives": [] }
}
```

`valueAnalysis` when a cheaper near-equal product exists (shape only):

```json
{ "bestFitProductId": 1, "bestValueProductId": 4, "isSmallGainUpgrade": true, "nearEqualScoreGap": 3,
  "cheaperAlternatives": [
    { "rank": 4, "productId": 4, "name": "...", "price": 50000.00, "score": 47.58,
      "scoreGap": 2.22, "priceDifference": 10000.00, "pricePerPoint": 4504.50 } ] }
```

- `items`: at most 3, best first. Fewer (or none) when fewer products qualify; still 200.
- `score`: 0..100, rounded half-up to 2 decimals. Ties: price ascending, then product id.
- `components`: only known criteria, in the order ram, storage, cpu, gpu, weight,
  refreshRate. `value` is GB, GB, tier (1..5), tier (1..5), kg or Hz. `normalizedValue`,
  `weight` and `contribution` (score points) are rounded to 4 decimals for display; the
  score uses full precision.
- `unknownCriteria`: criteria left out of the score because the value is unknown
  (a null field or a CPU/GPU name not in the tier table).
- `knownImportance / totalImportance`: sums of importance levels; `knownWeightShare` is
  their ratio (4 decimals). Products below 50% are removed and counted in
  `removedByKnownShare`.
- `specification`: stored facts; null fields are unknown. Prices of the Session 11.5
  dataset are development/test data, not market prices.

- `valueAnalysis` (Session 13): null when no product qualifies. The best fit is rank 1.
  `cheaperAlternatives` lists every ranked product (also beyond the Top 3; `rank` is its
  position) that is cheaper and at most `nearEqualScoreGap` (3) points lower, with the
  score gap, price difference and price per score point (2 decimals). `isSmallGainUpgrade`
  is true when the list is not empty. `bestValueProductId` is the cheapest of the best fit
  and its alternatives (ties: higher score, then lower id). The ranking in `items` is never
  changed by this analysis. With development/test prices the result is for testing only.

400 `application/problem+json` with `errors` for invalid JSON, out-of-range values or
excess decimals (for example `errors["Importance.Ram"]`).

### POST /api/recommendations/criteria
Status: Session 14. Public (no token for now; see ADR-026), read-only. Converts a
natural-language need into criteria in the POST /api/recommendations request shape. It
never ranks products: the client shows the criteria and sends them to
POST /api/recommendations. Only the user's text goes to the language model.

No language model provider is configured yet, so the endpoint currently answers 503.

```http
POST /api/recommendations/criteria
Content-Type: application/json

{ "text": "50 bin TL bütçem var, okul ve yazılım için kullanacağım, arada oyun oynarım, çok ağır olmasın." }
```

200 response (example):

```json
{
  "criteria": { "budgetMax": 50000, "minRamGb": null, "minStorageGb": null, "maxWeightKg": null,
                "importance": { "ram": 4, "storage": null, "cpu": 4, "gpu": 3, "weight": 4, "refreshRate": 3 } },
  "unsupported": []
}
```

- `criteria`: null fields are not applied; null importance levels default to 3 in the engine.
- `unsupported`: wishes no criterion covers (for example "battery life"); at most 10.
- 400 `application/problem+json` with `errors["Text"]` for missing, whitespace-only or
  longer than 1000 characters text.
- 502 `application/problem+json` when the model output is unusable: not JSON, unknown or
  duplicate fields, numbers as strings, values outside the POST /api/recommendations
  rules, or a budget although the text contains no digits. Values are never clamped.
- 503 `application/problem+json` when no provider is configured, the provider fails or
  it does not answer within 15 seconds.

### POST /api/recommendations/explanation
Status: Session 15. Public (no token for now; ADR-026/027), read-only. Same request body
as POST /api/recommendations. The service ranks first, then asks the language model to
explain that ranking, and returns both together so they always match.

No language model provider is configured yet, so the endpoint currently answers 503
whenever there is something to explain.

200 response (shape; `recommendation` is the full POST /api/recommendations response):

```json
{
  "recommendation": { "items": [ { "rank": 1, "productId": 13, "...": "..." } ], "summary": { "...": "..." },
                      "valueAnalysis": { "...": "..." } },
  "explanations": [ { "productId": 13, "explanation": "DELL PW514265 68.000 TL ile bütçeye uyuyor ..." } ],
  "valueComment": "Daha ucuz ve 3 puandan az geride bir seçenek yok."
}
```

- `explanations`: one per ranked product, in ranking order. Empty (and no model call)
  when no product qualifies.
- `valueComment`: null when there is no value analysis.
- The model receives only the criteria, the Top 3 stored facts, scores, score components
  and the value analysis. Prices are development demo prices.
- 400 `application/problem+json`: the same validation as POST /api/recommendations.
- 502 `application/problem+json` when the output breaks the schema or the fact check:
  a product missing, repeated or not in the Top 3, a value comment without a value
  analysis, an empty or over 1200-character text, or a number that does not appear in the
  input. The whole explanation is rejected; the ranking is still available from
  POST /api/recommendations.
- 503 `application/problem+json` when no provider is configured, the provider fails or it
  does not answer within 15 seconds.

---

## Authentication

Session 7 exposes the current profile through `GET /api/auth/me`. Session 8 adds
protected favorite routes. ProductResponseDto excludes favorites, user emails and roles.
UserProfile.Role controls the local Admin-check policy.

Preferred authentication provider: Supabase Auth.

Protected API requests:

```text
Authorization: Bearer <access_token>
```

ASP.NET Core validates the token and uses claims such as `sub` to identify the current user.

### GET /api/auth/me

Requires `[Authorize]`. Returns 200 with UserProfileResponseDto: id,
authProviderUserId, email, role and createdAt. Uses only the validated sub claim;
no user ID is accepted from the client. First access creates a local User profile
from a valid email claim. Existing profiles are reused without modifying their
email, role or creation time. Missing/invalid email for a new profile returns 403.

### GET /api/auth/admin-check

Requires authentication and the LocalAdmin policy. Returns 204 if the current
SQL profile has Role=Admin, otherwise 403. Provider roles and user_metadata do
not grant Admin privileges. No token, an invalid token or an expired token returns
401 with a Bearer challenge on both routes. No login/registration endpoint exists
in Pikwise. See [AUTHENTICATION.md](AUTHENTICATION.md) for setup and tests.

## Implemented: health endpoint (Session 1)

`GET /health` returns HTTP 200 with `text/plain` body `Healthy` when the host is
running. It checks application liveness only; SQL Server and other external
dependencies are not checked.
