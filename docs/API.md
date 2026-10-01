# API.md — Pikwise Draft Contracts

This file becomes the source of truth for public API behavior.

## Products

### GET /api/products
Purpose: list products.
Status: implemented in Session 5. Returns 200 with an array, including `[]` for
an empty catalog. Items are ordered by Id. Filtering and pagination remain future work.

Future query support:
- pagination
- sorting
- price
- brand
- RAM
- CPU/GPU
- storage

Response:
- collection of `ProductResponseDto`

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
specification, returns 204; an absent product returns 404. Lookup rows remain.

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

Names/CPU/GPU/OS/resolution must be nonblank and fit the mapped string lengths.
Price and stock must be nonnegative; FK IDs, RAM, storage and refresh rate must be
positive. ScreenSize is 0.01..999.99 inches; Weight is 0.001..999.999 kg. Price
fits decimal(18,2); price/screen allow two decimal places and weight allows three.
Names and specification strings are trimmed. A specification is required for
create/update. Missing fields with valid CLR defaults (price/stock/IsActive) use
those defaults; the JSON body itself and required strings/specification are required.

Malformed requests and invalid fields/references return 400 with
ValidationProblemDetails. Missing resources return 404. Concurrent removal or FK
conflicts during a write return 409. Unexpected failures return a generic 500
ProblemDetails with traceId; exception/SQL details are logged, not exposed.
Concurrent updates currently use last-write-wins; no row-version contract exists.

Authentication/authorization integration remains a later session. These CRUD
endpoints currently require no bearer token and are intended for local development.

---

## Comparison

Future:

```text
GET /api/products/compare?ids=1&ids=2&ids=3
```

Returns structured comparable product facts.

No LLM decision-making.

---

## Recommendation

Future endpoint receives structured needs or normalized criteria.

Output:
- ranked Top 3
- score breakdown
- value-for-money information
- verified product facts

---

## Authentication

Preferred authentication provider: Supabase Auth.

Protected API requests:

```text
Authorization: Bearer <access_token>
```

ASP.NET Core validates the token and uses claims such as `sub` to identify the current user.

## Implemented: health endpoint (Session 1)

`GET /health` returns HTTP 200 with `text/plain` body `Healthy` when the host is
running. It checks application liveness only; SQL Server and other external
dependencies are not checked.
