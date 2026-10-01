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
Input:
- route parameter: `int id`

Responses:
- 200: `ProductResponseDto`
- 404: product not found

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
dependencies are not checked. All product, comparison, recommendation and auth
contracts above remain planned.
