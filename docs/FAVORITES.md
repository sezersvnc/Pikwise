# Session 8 — Favorites API

## Endpoints

All routes require a valid Supabase user token. They accept only the product Id;
the local user Id comes from Session 7's current-profile use case.

| Request | Success | Other outcomes |
|---|---|---|
| GET /api/favorites | 200 with FavoriteResponseDto[], or [] | 401 invalid/missing token; 403 unresolved profile |
| POST /api/favorites/{productId} | 201 with FavoriteResponseDto | 400 nonpositive Id; 404 missing product; 409 duplicate/write conflict; 401/403 |
| DELETE /api/favorites/{productId} | 204 | 400 nonpositive Id; 404 no favorite belonging to the caller; 401/403 |

POST takes no request body. Client-supplied UserProfileId in a body or query does
not select the owner. The response contains CreatedAt and ProductResponseDto;
profile IDs, email, role and navigation graphs are excluded. Products are live
catalog data, not snapshots from the time they were favorited.

## Request flow

```text
Validated JWT sub
  -> FavoritesController [Authorize]
  -> FavoriteService
  -> UserProfileService (resolve/provision current profile)
  -> IFavoriteRepository / FavoriteRepository
  -> SQL Server Favorites
  -> FavoriteMapper / FavoriteResponseDto
```

Controller owns routes and HTTP outcomes. Application owns current-user resolution,
product-existence checks, UTC favorite timestamps and duplicate behavior. It reuses
IProductRepository for product lookup and ProductMapper for the nested product DTO.
Infrastructure owns owner-filtered queries, related-product loading and writes.
No schema change, migration or new package is required.

## SQL behavior and decisions

GET filters by the resolved UserProfileId before loading results. It uses
AsNoTracking and Include/ThenInclude for Product, Brand, Category and optional
LaptopSpecification. Results are ordered by CreatedAt descending, then ProductId
ascending for ties. Session 9 adds product catalog pagination; favorites retain this array contract.

POST checks that the product exists and inserts one join row using SaveChangesAsync.
Any existing product is eligible, including inactive/out-of-stock products;
additional availability rules have not been introduced. The composite primary key
prevents duplicates even during concurrent requests. SQL duplicate errors 2601/2627
return false to Application, which raises a controlled 409. Existing favorite
timestamps are preserved. SQL FK error 547 is translated into the existing
PersistenceConflictException and a 409 if a parent disappears during the write.

DELETE uses ExecuteDeleteAsync with both UserProfileId and ProductId in its WHERE
clause. One affected row means success; zero means 404, whether the product has
no favorite or belongs only to someone else's favorites. No separate product
lookup exposes another user's favorite. The operation deletes only the join row
and executes immediately without SaveChanges or change tracking. Parent records
remain. Product deletion still removes dependent favorites through the existing
database cascade. See [EF Core ExecuteDelete](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete).

## Verification and review

At Session 8 completion: zero build warnings/errors and 57 passing tests (6 unit, 51 integration).
At that stage, six SQL tests used PikwiseSession3Tests and 51 tests needed no SQL.
The new tests exercise JWT protection on every verb, unresolved-profile 403,
first-access provisioning, empty lists, DTO mapping, duplicate/conflict behavior,
concurrent adds, positive Id validation, missing products, ownership isolation,
parent preservation and cascade cleanup. SQL test data is removed after use.

The API was also started with local User Secrets. GET/POST/DELETE favorites without
a token each returned 401, and both favorite route templates appeared in OpenAPI.

[Pikwise.http](Pikwise.http) contains local requests using PIKWISE_ACCESS_TOKEN.
Use a Supabase user session access token locally. Real Supabase login and
GET /api/auth/me were manually verified on 2026-10-03, including the SQL profile
with Role=User. GET/POST/DELETE favorites with that real token have not yet been
manually verified; automated requests use temporary signed JWTs through the
production Bearer handler. Session 8 is complete. Session 9 product queries are documented in PRODUCT_QUERIES.md.

Start code review with Application/Favorites/Services/FavoriteService.cs,
Infrastructure/Favorites/FavoriteRepository.cs and Api/Controllers/FavoritesController.cs.
