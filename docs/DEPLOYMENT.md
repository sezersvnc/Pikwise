# DEPLOYMENT.md — Pikwise API on MonsterASP.NET (free plan)

Decision record: ADR-031. Never put a password, connection string or API key in this file,
in the repository, in screenshots or in chat.

## Target
| Item | Value |
|---|---|
| API address | `http://pikwise-api.runasp.net` (HTTPS after the support ticket, step 7) |
| Hosting | MonsterASP.NET free plan, Windows/IIS, .NET 10, 32-bit (x86) application pool, 256 MB RAM |
| Database | MSSQL `db72742`, SQL Server 2025, collation `SQL_Latin1_General_CP1_CI_AS` (ADR-030) |
| Site → database | Local access hostname (inside the datacenter); remote access stays **disabled** |

## Build the package (developer machine)
```bat
dotnet publish src/Pikwise.Api -c Release -r win-x86 --self-contained false -o publish/api
```
`win-x86` matches the 32-bit application pool and keeps only the needed native SQL client
library (about 20 MB, 7 MB zipped). `publish/` is in `.gitignore`. The package contains
`appsettings.json`, `appsettings.Development.json` and `appsettings.Production.json`
(non-secret values only) and an IIS `web.config` (in-process hosting).

## Database scripts
Generated into `publish/sql/` (ignored by git), both tested end to end on a temporary
local database:
- `01-schema.sql`: `dotnet ef migrations script --idempotent` over all migrations. Safe to run twice.
- `02-catalog-data.sql`: 10 brands, 1 category, 25 laptops, specifications and Icecat
  references from local PikwiseDb, with the same Ids. One transaction; it inserts nothing if
  the catalog tables already contain rows. No user profiles or favorites.

Remote access (port 1433) is often blocked on public networks, so the scripts are applied
through the control panel instead.

## Steps
1. **Database password.** Change the database password in the panel (the old one was
   exposed in a screenshot). Keep remote access disabled.
2. **Schema.** Databases → db72742 → Import SQL → `publish/sql/01-schema.sql`.
3. **Catalog.** Import SQL → `publish/sql/02-catalog-data.sql`. Overview must show 8 tables.
4. **Upload.** Websites → pikwise-api → Files (or WebFTP): delete the placeholder page in
   `wwwroot`, upload `publish/pikwise-api.zip` and extract it so `web.config` sits directly in
   `wwwroot`.
5. **Settings (secrets).** Websites → pikwise-api → Scripting → Environment variables, then
   Restart application:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__DefaultConnection` = the **Local access** connection string
     (`db72742.databaseasp.net`) with the new password
   - `Groq__ApiKey` = the Groq key (only if the language model endpoints should work)

   The issuer and CORS origins come from `appsettings.Production.json`. If the panel shows
   no environment variables, ask in the ticket (step 7); do not write secrets into
   uploaded files without a new decision.
6. **Check over HTTP** (no tokens yet):
   - `http://pikwise-api.runasp.net/health` → `Healthy`
   - `http://pikwise-api.runasp.net/api/products?pageSize=1` → JSON with `totalCount: 25`
   - `POST /api/recommendations` with body `{}` → three items and a summary
   A 500.30 page means the app did not start: usually a missing or wrong connection string.
   Check Websites → Logs.
7. **Support ticket:** ask for HTTPS on `pikwise-api.runasp.net` (free plan) and, if
   needed, where to set environment variables for an ASP.NET Core site.
8. **After HTTPS:** sign in through Supabase, call `GET /api/auth/me` once (creates the
   profile), then Databases → Run T-SQL:
   ```sql
   DECLARE @Email nvarchar(256) = N'your-email@example.com';
   UPDATE UserProfiles SET Role = 'Admin' WHERE Email = @Email;
   SELECT Id, Email, Role FROM UserProfiles WHERE Email = @Email;
   ```
   Then test `GET /api/auth/admin-check` (204), `POST /api/recommendations/criteria` and
   `/explanation` (Groq).
9. **Hand-off.** Give the frontend teammate `https://pikwise-api.runasp.net`. When the frontend
   has its own address, replace the localhost origins in `appsettings.Production.json`
   (or set `Cors__AllowedOrigins__0`) and redeploy.

## Rules
- No signed-in endpoint is used over plain HTTP: bearer tokens would travel unencrypted.
- Secrets live only in the panel's environment variables (and the owner's User Secrets locally).
- Remote database access is enabled only for a specific task and disabled right after.
- Prices are development/test data (ADR-025); do not present them as market prices.
- Redeploy: run `dotnet publish` again, upload the new files, Restart application. Apply
  new migrations with a fresh idempotent script before uploading code that needs them.
