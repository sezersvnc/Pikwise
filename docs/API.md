# API.md — Pikwise Draft Contracts

This file becomes the source of truth for public API behavior.

## Products

### GET /api/products
Purpose: list products.

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
- 404: empty response when the product does not exist

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

### PUT /api/products/{id}
Input:
- route `id`
- `UpdateProductRequestDto`

### DELETE /api/products/{id}
Input:
- route `id`

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
