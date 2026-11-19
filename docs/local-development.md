# Local development

## Run with Docker Compose

Prerequisites: Docker Desktop/Engine with Compose v2. PowerShell 7 is needed for the supplied setup and smoke scripts. The containers build the API and client; installing a local SDK or Node is optional.

From the repository root:

```powershell
./infra/Initialize-Local.ps1
docker compose up --build -d
```

Open http://localhost:8080. The first image build downloads dependencies. If the page appears before the API is ready, refresh after `/health/ready` returns 200.

The initializer creates an ignored `.env` with four independently generated passwords and never overwrites an existing file. Read it locally to sign in:

- Chef: `chef@dinder.local`, password from `DINDER_CHEF_PASSWORD`.
- Member: `member@dinder.local`, password from `DINDER_MEMBER_PASSWORD`.

On other shells, copy `.env.example` to `.env` and replace every placeholder. Identity passwords must contain at least 12 characters, including uppercase, lowercase, a number, and a non-alphanumeric character. For database passwords use generated alphanumeric/base64 characters without connection-string delimiters such as semicolons. Do not commit `.env` or reuse these development accounts outside localhost.

The API applies committed EF migrations and seeds an `Our kitchen` household, two users, their memberships, and three original sample recipes only when running in **Development** with the corresponding flags enabled. It does not reset passwords, recreate an active round, or overwrite votes on restart. Changing bootstrap passwords in `.env` does not change existing Identity passwords. The seed is intentionally not a production provisioning mechanism.

Local addresses:

- App and proxied API: `http://localhost:8080`.
- Liveness: `http://localhost:8080/health/live`.
- Database readiness: `http://localhost:8080/health/ready`.
- PostgreSQL: `localhost:5433`, database `dinder`, user `dinder`, password `DINDER_DATABASE_PASSWORD`.

Web and database ports bind only to loopback. The API has no host port in Compose. Keep this Development configuration local; HTTPS, trusted forwarding, production provisioning, secrets storage, and backup/restore are later deployment work.

```powershell
docker compose ps
docker compose logs --tail 50 api
docker compose stop
docker compose start
```

Named volumes retain the database and Identity data-protection keys. `docker compose down` removes containers but retains those volumes. Avoid `down -v` unless you deliberately want to permanently erase all local accounts, rounds, and votes. No reset is needed for ordinary testing.

### Workstations with TLS inspection

This workstation uses Norton Web/Mail Shield. The first Linux container build could not validate intercepted package-registry certificates even though Windows could. The Dockerfiles accept an optional `build_ca` secret to match an already-trusted workstation root **in build stages only**. No certificate is copied to runtime images and TLS verification remains enabled.

If you encounter this specific error, inspect your trusted certificate and supply its thumbprint:

```powershell
Get-ChildItem Cert:\LocalMachine\Root | Where-Object Subject -Like '*Norton*' | Format-List Subject, Thumbprint
./infra/Initialize-BuildTrust.ps1 -Thumbprint '<the inspected existing trusted root thumbprint>'
docker compose -f compose.yaml -f .tools/compose.trust.yaml up --build -d
```

The certificate and override are ignored local files. Plain `docker compose` remains the default for machines without TLS inspection; do not import another person's workstation certificate. For host npm on Node 24, `$env:NODE_USE_SYSTEM_CA = '1'` uses Windows' existing trust store. Do not disable certificate validation.

## Work on source without rebuilding containers

Install .NET 10 SDK and Node 24 LTS. `global.json` permits the newest installed .NET 10 feature band. An agent-installed SDK may exist at `.tools/dotnet/dotnet.exe`; it is ignored and optional. Replace `dotnet` below with that path if the system SDK is older.

```powershell
docker compose up -d db
# Stop the container API first if it is running; one API instance performs local seeding.
docker compose stop api
$localSettings = @{}
Get-Content .env | ForEach-Object {
    if ($_ -match '^([^#=]+)=(.*)$') { $localSettings[$matches[1]] = $matches[2] }
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__Dinder = "Host=localhost;Port=5433;Database=dinder;Username=dinder;Password=$($localSettings.DINDER_DATABASE_PASSWORD)"
$env:Database__MigrateOnStartup = 'true'
$env:Bootstrap__Enabled = 'true'
$env:Bootstrap__ChefPassword = $localSettings.DINDER_CHEF_PASSWORD
$env:Bootstrap__MemberPassword = $localSettings.DINDER_MEMBER_PASSWORD
dotnet run --project src/Dinder.Api --urls http://localhost:5080
```

In another terminal:

```powershell
cd src/Dinder.Web
npm ci
npm run dev
```

Use Vite's printed localhost URL (normally port 5173). Vite proxies `/api` to port 5080, preserving same-origin browser requests. Do not browse the container web while testing the host API.

## Test and build

```powershell
dotnet test Dinder.slnx
dotnet build Dinder.slnx -c Release
cd src/Dinder.Web
npm ci
npm run build
```

After the initial package restore, the tests themselves use no Docker, external network, browser, or manually supplied credentials. To verify offline after restoring, use `dotnet test Dinder.slnx --no-restore`. Domain tests are pure; API tests start ASP.NET in-process with an isolated SQLite in-memory database, real Identity cookies, and test-owned credentials. PostgreSQL migrations and provider behavior need the separate local integration check:

```powershell
# From the repository root, with Compose running. Mutates local sample votes.
./infra/Test-Local.ps1 -RestartApi
```

The smoke script starts a round if needed, exercises Chef/member authorization and CSRF, cycles the first recipe through Yes/No/Skip, and checks that the vote and session survive an API restart. It leaves a Member Skip vote on that sample recipe. It is not part of the ordinary unit-test command.

## Migrations

```powershell
dotnet tool restore
dotnet ef migrations add DescriptiveName --project src/Dinder.Api --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Dinder.Api
```

The initial migration creates Identity tables and `households`, `household_members`, `recipes`, `planning_rounds`, and `votes`, including tenant-aware foreign keys and the unique active-round index. It contains no accounts, passwords, or sample records. The separate development seed inserts those at startup.

The local PostgreSQL initializer creates a non-superuser `dinder` role with permission to create tables in `public`; this lets the API apply local migrations. It runs only on a fresh database volume. Changing its script or passwords later does not retroactively change an existing database. Production will need separately controlled migration credentials and a reviewed runbook.

## Manual browser verification

1. Sign in as Chef. On a fresh database, confirm the recipe list shows three samples and voting is unavailable until a round starts.
2. Start a planning round. Confirm the active status appears and the start button disappears.
3. Open recipe details and cast a Yes vote. Change it to No, then Skip; refresh and confirm the final choice persists.
4. Sign out and sign in as Member. Confirm the Member has independent votes and no round-start control.
5. Cast a vote, reload, sign out and back in; confirm persistence. Try the smoke script for direct API authorization checks.
6. At a 390 × 844 viewport, check sign-in, the recipe list, and all three voting buttons. No horizontal scrolling should be needed.

There is intentionally no deck, round-closing UI, import flow, recommendation, or final plan in this milestone. A No-voted recipe remains available in the explicit recipe list/detail view so a member can change the vote; deck exclusion and Skip resurfacing are Milestone 3.
