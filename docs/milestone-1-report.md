# Milestone 1 completion report

Verified on September 20, 2026. Scope: the updated [first autonomous run](first-autonomous-run.md), not the older, broader suggested milestone at the bottom of the product brief.

## Result

The local application runs at http://localhost:8080. A provisioned Chef can sign in, access the household, and start one active planning round. Chef and Member accounts can independently cast persistent Yes/No/Skip votes on three sample recipes and change them in recipe details. API membership checks and database constraints enforce household boundaries.

The complete architectural inventory is [30 decisions with rationale and tradeoffs](milestone-1-decisions.md). [ADR 0003](decisions/0003-milestone-1-foundation.md) records the durable foundation choice; existing ADRs 0001/0002 establish the stack and authentication.

## Files delivered

- Repository/build: `.gitignore`, `.gitattributes`, `.dockerignore`, `.env.example`, `global.json`, `Directory.Build.props`, `Dinder.slnx`, `.config/dotnet-tools.json`, `compose.yaml`, and updated `README.md`.
- API host: `src/Dinder.Api/Dinder.Api.csproj`, `Program.cs`, `appsettings.json`, and `Dockerfile`.
- API domain: `Domain/Models.cs`, `Domain/PlanningRules.cs`.
- API persistence: `Data/DinderDbContext.cs`, `Data/DevelopmentSeed.cs`, `Data/Migrations/20260920154745_InitialFoundation.cs`, its `.Designer.cs`, and `DinderDbContextModelSnapshot.cs`.
- API features/contracts: `Features/PlanningService.cs`, `Features/ApiEndpoints.cs`, `Features/Contracts.cs`.
- Web project: `src/Dinder.Web/package.json`, `package-lock.json`, `tsconfig.json`, `vite.config.ts`, `index.html`, `Dockerfile`, `src/api.ts`, `src/main.tsx`, `src/styles.css`, and `src/components/{SignIn,RecipeList,RecipeDetails}.tsx`.
- Tests: `tests/Dinder.Api.Tests/Dinder.Api.Tests.csproj`, `PlanningRulesTests.cs`, `ApiTests.cs`, `TestApplication.cs`.
- Local operations: `infra/Initialize-Local.ps1`, `Initialize-BuildTrust.ps1`, `Test-Local.ps1`, `init-database.sh`, and `nginx.conf`.
- Documentation: `docs/local-development.md`, `api.md`, `milestone-1-decisions.md`, `milestone-1-report.md`, `decisions/0003-milestone-1-foundation.md`, updated `architecture.md` and `current-state.md`, and browser screenshots under `docs/screenshots/`.

Local-only artifacts: generated `.env`, downloaded .NET SDK and build certificate/override under `.tools`, dependency/build output, and Docker named volumes. They are ignored. A local Git repository was initialized on `main`; no commit or remote was created.

## Commands run and results

The system initially had .NET 9, so .NET SDK 10.0.401 was installed into ignored `.tools/dotnet` using Microsoft's official installer. The `dotnet` commands below were executed through `.\.tools\dotnet\dotnet.exe`; migration tooling also used that directory as `DOTNET_ROOT` and the first PATH entry. Web commands ran from `src/Dinder.Web`.

Successful checks:

```text
.\.tools\dotnet\dotnet.exe test Dinder.slnx --nologo
  15 passed, 0 failed, 0 skipped.
.\.tools\dotnet\dotnet.exe test Dinder.slnx --no-restore --nologo
  15 passed, 0 failed, 0 skipped after the migration/formatting changes.
.\.tools\dotnet\dotnet.exe build Dinder.slnx -c Release --no-restore --nologo
  Passed, 0 warnings, 0 errors.
.\.tools\dotnet\dotnet.exe tool restore
  Restored EF tooling 10.0.12.
.\.tools\dotnet\dotnet.exe ef migrations add InitialFoundation --project src/Dinder.Api --output-dir Data/Migrations
  Built and generated initial migration plus model snapshot.
.\.tools\dotnet\dotnet.exe format whitespace Dinder.slnx --no-restore
  Passed.
npm install --fetch-retries=0
  Passed with NODE_USE_SYSTEM_CA=1; dependency audit reported 0 vulnerabilities.
npm install --save-dev --save-exact prettier --fetch-retries=0
  Passed with NODE_USE_SYSTEM_CA=1; dependency audit reported 0 vulnerabilities.
npm run build
  TypeScript and Vite passed, including the final component split.
npm run format:check
  All matched files passed.
./infra/Initialize-Local.ps1
  Generated independent random local credentials without printing them.
docker compose config --quiet
  Passed.
docker compose -f compose.yaml -f .tools/compose.trust.yaml up --build -d
  Built and started PostgreSQL, API, and web; migration/seed applied to a fresh volume.
./infra/Test-Local.ps1 -RestartApi
  Passed authorization, CSRF, Yes/No/Skip persistence, foreign-household rejection,
  and persisted session/vote after API restart.
docker compose exec -T db psql -U postgres -d dinder -c "SELECT rolname, rolsuper, rolcreatedb, rolcreaterole FROM pg_roles WHERE rolname = 'dinder';" -c "SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'planning_rounds';" -c "SELECT choice, count(*) FROM votes GROUP BY choice ORDER BY choice;"
  Inspected pg_roles, pg_indexes, and persisted vote counts.
  App role has no superuser/create-database/create-role privileges.
  PostgreSQL contains the unique partial active-round index and saved votes.
git check-ignore .env .tools/dotnet/dotnet.exe src/Dinder.Web/node_modules/react/package.json
  All three paths correctly ignored.
```

Initial failures and resolutions:

- Sandbox denied Docker engine access, .NET first-use writes, package-network access, and Vite's child process. Those operations were retried through the approval flow and succeeded.
- An initial restore of older EF/SQLite test dependencies failed vulnerability auditing. Current .NET/EF 10.0.12 and Npgsql 10.0.3 restored and tested successfully; no audit warning was suppressed.
- npm initially failed `UNABLE_TO_VERIFY_LEAF_SIGNATURE`; Node's existing Windows trust store resolved it.
- Plain `docker compose up --build -d` failed package restore with an incomplete Linux certificate chain. The optional build-only trusted-root secret resolved this workstation's Norton TLS inspection. See the setup guide; no TLS verification was disabled.
- The first smoke script incorrectly expected a missing-token rejection while its PowerShell WebRequestSession still retained a previous token header. Explicitly removing that header fixed the test; the missing-token request then returned 400. The in-process API antiforgery test had already passed.

## Test inventory

**Six pure domain cases:** membership must match household and user; only Chef starts a round and active rounds cannot duplicate; each of Yes/No/Skip is accepted; missing, foreign, closed, and invalid vote targets are rejected. These use no database or network.

**Nine in-process API cases:** anonymous access; CSRF/login/logout behavior; Member versus Chef permissions and duplicate-round response; three vote persistence/update cases; foreign-household/recipe rejection; malformed/missing/numeric choices and closed rounds; database rejection of a cross-household vote. These use real ASP.NET middleware and Identity with isolated in-memory SQLite. No Docker, browser, external calls, or user-supplied test secrets are involved.

**PostgreSQL/Compose checks:** fresh EF migration and seed; HTTP smoke script; database role/index inspection; vote and session survive API restart. These need local integration infrastructure and are intentionally separate from `dotnet test`.

**Browser checks:** Chef sign-in, fresh household, round creation, Yes → No → Skip updates, persistence after reload, sign-out, independent Member view/voting, desktop layout, and 390 × 844 phone layout. DOM measurements confirmed 390-pixel content width at the 390-pixel viewport. Browser checks were performed with the app's browser tooling rather than adding a browser-test runtime dependency.

## Screenshots

- [Sign-in](screenshots/sign-in.png).
- [Final kitchen overview](screenshots/kitchen.png).
- [Phone sign-in](screenshots/phone-sign-in.png).
- [Chef before starting a round](screenshots/chef-before-round.png).
- [Chef persisted vote](screenshots/chef-vote.png).
- [Member phone recipe list](screenshots/member-phone-list.png).
- [Member phone vote](screenshots/member-phone-vote.png).

## Limitations and next verification

The app deliberately stops before URL import, deck rules, recommendations, finalized plans, and public deployment. The only household/account provisioning is Development bootstrap. Round close exists but does not yet finalize a plan or snapshot the round's votes. There is no account recovery, invitation flow, production runbook, or committed browser regression suite. Detailed operational/security tradeoffs and concurrency semantics are recorded in the decision inventory and [current state](current-state.md).

For a useful human review: open the app, sign in using `.env`, inspect `PlanningRules` and `DinderDbContext`, change a vote from recipe details, reload, and compare the Chef and Member accounts. Read the decision inventory alongside those files, then use the [manual verification steps](local-development.md#manual-browser-verification) before starting Milestone 2.
