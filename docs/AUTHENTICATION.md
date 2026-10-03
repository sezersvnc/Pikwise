# Session 7 — Authentication foundation

Supabase Auth issues user access tokens. Pikwise validates them and owns its local
profile and authorization rules. There is no Pikwise password, login, registration
or token-issuance endpoint. The application database remains SQL Server.

## Configuration

Set the issuer outside tracked configuration, using the real project URL:

```powershell
dotnet user-secrets set "Authentication:Supabase:Issuer" "https://<project-ref>.supabase.co/auth/v1" --project src/Pikwise.Api
```

The current local project issuer has been saved in User Secrets. For deployment,
set `Authentication__Supabase__Issuer`. Audience defaults to `authenticated` and
can be set through `Authentication__Supabase__Audience`. Missing/invalid issuer or
blank audience fails startup with a configuration-key-only message. Issuer must
be HTTPS, end in `/auth/v1`, and have no credentials, query or fragment.

The API downloads public keys from `<issuer>/.well-known/jwks.json`. Supabase's
asymmetric ES256/RS256 signing keys are supported. Legacy HS256 secrets and
anon/service-role API keys are not accepted as user access tokens. No publishable
key, service-role key or signing secret is needed by this API for validation.
The existing SQL connection still comes from User Secrets or environment variables.

## Request flow and layer ownership

```text
Authorization: Bearer <access token>
  -> API JWT middleware: signature, issuer, audience, expiration, subject and provider role
  -> [Authorize] / local authorization policy
  -> AuthController
  -> UserProfileService + ICurrentUser
  -> IUserProfileRepository / UserProfileRepository
  -> SQL Server UserProfiles
  -> UserProfileResponseDto
```

API owns Bearer registration, claim access through HttpCurrentUser and HTTP
responses. Authentication runs before authorization. Application owns the current
profile use case and its interfaces without depending on HttpContext or JWT types.
Infrastructure performs async profile reads/inserts and handles duplicate subject
inserts. Domain remains free of HTTP and EF dependencies.

JWT claim mapping is disabled so `sub` stays `sub`. A user access token must have
exactly one nonblank subject of at most 128 characters and one provider `role`
equal to `authenticated`. Signature, issuer, audience and lifetime are mandatory;
clock skew is 30 seconds. Token validation errors are not included in responses.

## Protected endpoints

| Request | Result |
|---|---|
| `GET /api/auth/me` with a valid user token | 200 with the caller's local profile |
| Either endpoint without a token or with an invalid/expired token | 401, Bearer challenge |
| `GET /api/auth/admin-check` with local Role=User | 403 |
| `GET /api/auth/admin-check` with local Role=Admin | 204 |
| First profile access without a valid email claim | 403, no profile inserted |

The profile response contains Id, AuthProviderUserId, Email, Role and CreatedAt.
Neither endpoint receives UserProfileId. The subject is taken only from the
validated principal. A query parameter claiming another user's Id is ignored.
No route exposes arbitrary profiles or their favorites.

On first access, Application provisions a profile from `sub` and a nonblank,
valid email claim up to 254 characters, sets UTC CreatedAt and assigns Role=User.
Later access looks up the exact subject and preserves local email, role and
creation time. Email synchronization and phone-only profile onboarding are future
decisions; an existing profile can be resolved without an email claim.

Supabase's `role` is a provider database role, not a Pikwise permission. The
LocalAdmin policy reads UserProfile.Role from SQL Server on each request. Token
roles and editable user_metadata cannot promote a profile. There is no API for
changing local roles in this session.

The unique subject index handles simultaneous first requests. On SQL duplicate
errors 2601/2627, the repository detaches its losing insert and retrieves the
existing profile; unrelated failures propagate. No schema migration is needed.

## Verification and local requests

All 44 tests pass: 6 unit and 38 integration tests. Five integration tests require
the dedicated PikwiseSession3Tests database; 39 tests run without SQL Server.
The Release build has zero errors and warnings.

Authentication tests exercise the production Bearer handler with temporary EC/RSA
keys exposed through an in-memory JWKS transport. They verify valid JWTs, invalid
signatures, issuer/audience mismatches, expiration, future validity, missing
expiration, unsigned/HS256 tokens, invalid/duplicate subjects and service-role
tokens. They also verify profile reuse/isolation, email provisioning rules and
local 403/204 authorization. The SQL test covers concurrent first requests,
duplicate recovery, persistence, separate identities and local role changes.

The real project's public JWKS was fetched successfully and contains an ES256 key.
The API was also started with local User Secrets: /health returned 200, protected
endpoints without a token returned 401, an invalid token returned 401 and OpenAPI
listed both auth routes. A real Supabase user login/token has not been exercised.

Use [Pikwise.http](Pikwise.http) with VS Code REST Client or import equivalent
requests into Postman. Its auth requests read `PIKWISE_ACCESS_TOKEN` from the local
process environment. Supply an access token from a Supabase user session locally;
do not save tokens in tracked files. GET /api/auth/me should return 200, and
admin-check should return 403 for a default User profile. Delete the authorization
header to verify 401. The OpenAPI JSON is at `/openapi/v1.json` in Development.

## Limits and next session

JWT verification accepts an otherwise valid token until it expires (plus clock
skew); it does not call Supabase to check session revocation on every request.
IdentityModel caches keys, refreshes automatically every 10 minutes and requests
refresh for unknown signing keys with a one-minute refresh throttle. Supabase also
caches its public JWKS, so key changes are not instantaneous. Use HTTPS when
deploying the API.

Product CRUD keeps its existing public development contract; protecting product
writes with a local Admin policy is a separate decision. Favorites API belongs
to Session 8 and has not been implemented. Filtering, comparison and recommendation
remain future sessions. Session 8 has not started.

Sources: [Supabase JWT verification](https://supabase.com/docs/guides/auth/jwts),
[Supabase signing keys](https://supabase.com/docs/guides/auth/signing-keys),
[ASP.NET Core JWT Bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).
