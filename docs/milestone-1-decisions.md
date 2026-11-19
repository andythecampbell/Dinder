# Milestone 1: architectural decisions, reasons, and tradeoffs

This is the implementation decision inventory requested for the first autonomous milestone. It distinguishes the approved baseline from implementation choices. None of these choices adds an external service or changes the approved technology stack. See [ADR 0003](decisions/0003-milestone-1-foundation.md) for the durable summary.

## Read the code in this order

1. `src/Dinder.Api/Domain/PlanningRules.cs`: membership, Chef authorization, round creation, and vote eligibility.
2. `src/Dinder.Api/Domain/Models.cs` and `Data/DinderDbContext.cs`: persisted state and database constraints.
3. `src/Dinder.Api/Features/PlanningService.cs`: load scoped resources, apply rules, persist changes.
4. `src/Dinder.Api/Features/ApiEndpoints.cs`, `Contracts.cs`, and `Program.cs`: HTTP contracts, Identity, antiforgery, and middleware.
5. `tests/Dinder.Api.Tests`: pure rules plus real HTTP/Identity tests.
6. `src/Dinder.Web/src/api.ts`, `main.tsx`, and `components/`: browser transport, screen state, and presentation.
7. `compose.yaml`, Dockerfiles, and `infra/`: local packaging and verification.

## Scope and structure

### 1. Deliver seeded voting before URL import

**Decision:** Follow the newly supplied Milestone 1/run brief: foundation, authentication, household access, round creation, and votes on three development recipes.

**Why:** It proves the risky integration points—identity, tenancy, persistence, and browser requests—before introducing an untrusted URL fetcher. This is a sequencing refinement to the original product brief.

**Tradeoff:** The app cannot yet accept a household's real recipes. Import is Milestone 2; deck behavior is Milestone 3. No placeholder importer, scoring, or final-plan machinery is present.

### 2. One modular API project, with a separate SPA

**Decision:** Preserve the documented `src/Dinder.Api`, `src/Dinder.Web`, `tests/Dinder.Api.Tests`, `docs`, and `infra` structure. Domain, data, and feature folders establish boundaries inside one API assembly.

**Why:** A small monolith is easy to navigate and deploy. The separation between rules, persistence, and endpoints is useful now without requiring multiple class-library projects.

**Tradeoff:** Folder boundaries are conventions rather than compiler-enforced dependency boundaries. Split assemblies only if future features make those boundaries hard to maintain.

### 3. .NET 10 LTS and conventional frontend tooling

**Decision:** Target .NET/EF Core 10, React 19, TypeScript 5.9, Vite 7, Node 24, and PostgreSQL 17. Pin direct .NET package versions; use npm's committed lockfile. `global.json` allows current .NET 10 feature bands.

**Why:** This is a new application; an LTS baseline avoids starting on the locally installed .NET 9 SDK near its support deadline ([Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)). Vite supplies the normal SPA build/dev-server workflow. PostgreSQL 17 is sufficient for the approved relational model.

**Tradeoff:** Host development needs a newer SDK. Docker can build without it. Container image tags permit patch refreshes rather than fully reproducible digest-pinned images; production pinning/update policy belongs to the deployment milestone.

### 4. A small application service, no generic framework

**Decision:** `PlanningRules` contains pure rules; `PlanningService` orchestrates membership lookups, scoped EF queries, clock access, and persistence. Minimal API endpoints handle transport and DTO mapping.

**Why:** Rules can be tested without the database, while the orchestration remains visible in a short class. EF already supplies a unit of work.

**Tradeoff:** There is no generic repository, mediator, command bus, or interface for every class. Future import/scoring/notification services should be introduced when their milestones need them, using the existing domain/feature boundary.

## Identity and request security

### 5. ASP.NET Core Identity with local accounts

**Decision:** Implement approved ADR 0002 with PostgreSQL-backed Identity password hashing, cookies, lockout, and no public registration or external provider.

**Why:** It uses the .NET framework's existing authentication primitives and keeps the API authoritative.

**Tradeoff:** Invitations, password reset, email confirmation, MFA, and non-development account administration do not exist yet. The test household is not a production account-management solution.

### 6. Provision the first household during explicit development bootstrap

**Decision:** An idempotent Development-only seed creates `Our kitchen`, one Chef, one Member, memberships, and original sample recipes. The Chef accesses that household after login rather than completing a creation wizard.

**Why:** The acceptance criterion allows creation **or access**. Provisioning makes the local slice usable without adding signup, invitation, or multi-household UX policy.

**Tradeoff:** There is no general create-household endpoint or UI. Password changes in `.env` do not reset existing accounts. Bootstrap is not safe to run concurrently in multiple application instances and is deliberately limited to a single local API.

### 7. Household-specific roles, checked against current database state

**Decision:** `HouseholdMember.Role` carries `Chef` or `Member`. Every household request resolves membership for the authenticated user. Chef authorization is checked before starting a round.

**Why:** A future user can be Chef in one household and Member in another; a global `Chef` claim would be wrong. Database lookups also avoid stale role claims in long-lived cookies.

**Tradeoff:** Each request incurs a membership query. No role cache is needed at household scale. Standard Identity role tables remain framework-managed but are not used for household permissions.

### 8. Same-origin session cookies

**Decision:** nginx serves static web assets and proxies `/api`; Vite provides the equivalent proxy during source development. Session cookies are HttpOnly, SameSite=Strict, and eight-hour sliding sessions. Secure is mandatory outside Development/Testing.

**Why:** The browser never needs to store a bearer token, and cross-origin credential/CORS configuration is unnecessary.

**Tradeoff:** Local HTTP requires a development-only Secure exception. External clients and a future public deployment need their own reviewed transport/forwarding setup. The current Compose stack binds only to loopback.

### 9. Explicit antiforgery protection, including login

**Decision:** Require the antiforgery cookie and `X-CSRF-TOKEN` on every API mutation. The client obtains a fresh token before each mutation, so sign-in/out identity changes cannot leave a stale token.

**Why:** Cookie authentication needs CSRF protection; SameSite is an additional safeguard. Login also changes browser identity and is protected.

**Tradeoff:** Each mutation uses an extra small request. A token cache could remove that later but would need careful invalidation. Missing tokens receive a recoverable 400 response, not a silent retry of a mutation.

### 10. Baseline login abuse controls

**Decision:** Identity locks an account after five failures for five minutes; a built-in fixed-window limiter permits 20 login requests per minute per connection address. Error messages do not reveal whether an account exists.

**Why:** These controls are available in the framework and need no new service.

**Tradeoff:** Behind the current nginx proxy, clients share a proxy address and therefore share the limiter bucket. That is acceptable locally; trusted forwarding/per-client rate limits require review before public exposure. The limiter is in-memory and resets on restart.

### 11. Persist cookie encryption keys locally

**Decision:** Store ASP.NET data-protection keys in a named volume, under the non-root API user's home.

**Why:** Restarting or replacing the API container should not invalidate every session.

**Tradeoff:** The keys are not encrypted at rest in this local setup. Anyone with host/volume access is trusted; key protection, backup, and production permissions are later operational work. Logout removes the browser cookie; it is not a server-side revocation list for already stolen cookies.

## Data model and consistency

### 12. Explicit tenant predicates plus composite foreign keys

**Decision:** Every domain record is scoped by `household_id`. Queries explicitly constrain household and user where relevant. Votes reference membership, round, and recipe through composite tenant-aware foreign keys.

**Why:** Request checks prevent unauthorized access, while relational constraints prevent structurally cross-household votes even if application code makes a mistake.

**Tradeoff:** Composite keys add indexes and mapping code. There is no PostgreSQL row-level security or implicit global EF tenant filter; every new read path must retain explicit membership and tenant checks. Database constraints protect relationships, not read authorization.

### 13. UUID domain identifiers and UTC timestamps

**Decision:** Household, recipe, and round IDs use UUIDs. Identity keeps its standard string user ID. Timestamps use `DateTimeOffset` and `TimeProvider.System`; PostgreSQL stores them as `timestamp with time zone`.

**Why:** Resources can be identified before insert, identity is not coupled to a custom key scheme, and clock-dependent code can accept a controlled clock.

**Tradeoff:** UUID indexes are larger than integer indexes. Random IDs are not an authorization boundary. User-specific week/timezone rules are deferred; rounds currently record start time, not a calendar schedule.

### 14. Domain snake_case, standard Identity schema

**Decision:** Domain tables/columns use snake_case, including the required `household_id`. Identity retains its conventional `AspNet*` tables and property names. Role and vote enum values are stored as readable strings.

**Why:** This follows the stated household naming convention without adding a naming-conventions package or rewriting framework mappings.

**Tradeoff:** Database naming styles are mixed at the framework/domain boundary. Renaming a persisted enum value requires a migration. External writes must respect the model; enum validity is checked by application code rather than a database check constraint.

### 15. One active round per household

**Decision:** Store an active flag and a partial unique index on household ID where active. Domain logic gives a helpful error; PostgreSQL is the final arbiter if requests race.

**Why:** A read-then-insert check alone cannot prevent concurrent active rounds.

**Tradeoff:** Closing is implemented as a single conditional update on the active flag, so a double close cannot return 204 twice. Vote writes take a row lock on the round for the duration of their transaction, so a close cannot interleave between the activity check and the vote insert. Finalization snapshots and the recommendation/final-plan rules remain Milestone 3; closing a round only releases the one-active-round constraint.

### 16. One current vote per user, recipe, and round

**Decision:** Use a composite primary key across household, round, recipe, and user. A PUT replaces the current Yes/No/Skip choice and updates its timestamp.

**Why:** The data directly represents the product rule and cannot accumulate duplicate preferences from repeated clicks or retries. Votes persist across rounds without requiring an event log.

**Tradeoff:** Preference changes within a round are not an append-only audit trail. Concurrent updates are last-write-wins; simultaneous first inserts can return 409 and ask for refresh. Full preference-change history can be added when a concrete product requirement justifies it.

### 17. Keep voting private at the current API boundary

**Decision:** The kitchen response contains only the caller's active-round votes. Submitted requests never provide the voter ID; it comes from the authenticated session. Foreign/missing resources return 404.

**Why:** Members make independent choices, and cross-household existence should not be disclosed. Chef aggregate summaries are a later milestone.

**Tradeoff:** Even the Chef cannot yet inspect other members' choices or participation. That requires a deliberate summary contract when recommendations are implemented.

### 18. Minimal sample recipe data

**Decision:** Store title, description, estimated minutes, ordered ingredient/instruction string arrays, and a development-seed marker. Samples are original, clearly labeled, and use CSS illustrations rather than fetched images.

**Why:** This supports a usable detail/voting screen without pretending URL-import provenance has been implemented. Native PostgreSQL text arrays preserve order without child tables for content that is currently read as a whole.

**Tradeoff:** Import metadata, canonical URLs, source attribution, images, normalized times, and cuisines require a Milestone 2 migration. Ingredient quantities are unstructured; arrays are less suitable for grocery-list queries, which are out of scope.

### 19. Restrict deletes for domain history

**Decision:** Domain relationships use restricted deletion rather than cascading removal of household voting history.

**Why:** An accidental user/recipe/household delete should not silently erase related votes.

**Tradeoff:** Future account/household deletion needs an explicit lifecycle policy and ordered cleanup or archival. There is no deletion feature now.

## Contracts and browser design

### 20. Small REST contracts and Problem Details

**Decision:** Use typed request/response DTOs and conventional GET/POST/PUT endpoints. Enum inputs are named strings; omitted, null, numeric, and unknown votes are rejected. Static API documentation describes the contract.

**Why:** Contracts remain independent of EF tracking and avoid over-posting identity or tenancy fields. Formal API versioning and an OpenAPI UI are unnecessary for one private client.

**Tradeoff:** TypeScript mirrors the C# contracts manually. A generated client would reduce drift later but would add tooling now. Minimal API parsing can return a bare 400 before application code; the client has fallback error messages.

### 21. One household screen with explicit recipe details

**Decision:** The SPA holds session, kitchen, selected recipe, busy/error, and notice state in React. Sign-in, recipe list, and detail/voting presentation are separate components. No router, global store, or query library is introduced.

**Why:** This milestone has one small flow. Hooks and a typed `api.ts` are sufficient, while component boundaries keep the code navigable.

**Tradeoff:** Selection is not deep-linked and resets on reload; the UI chooses the first returned membership. The data model supports many households, but the UI deliberately does not yet offer household switching. A router and query cache become reasonable when more screens exist.

### 22. Wait for confirmed persistence and refresh authoritative data

**Decision:** Disable mutation controls while saving and refetch the kitchen after successful changes. Show explicit success/error messages; provide a Refresh button for changes made by another member.

**Why:** The UI should never label an unsaved vote as persisted. This keeps failure behavior easy to understand and avoids optimistic rollback code.

**Tradeoff:** Mutations take more requests, and another person's round-start action does not automatically update an already open screen. Real-time connections or polling are unnecessary for this milestone.

### 23. A responsive recipe list rather than a partial deck

**Decision:** Show all samples and allow vote changes only through explicit recipe details. Use labeled controls, visible keyboard focus, pressed state for selected choices, and live feedback. Stack cards and controls on narrow screens.

**Why:** It proves the slice without prematurely choosing deck traversal, Skip ordering, or target-completion UX. Recipe detail explicitly supports changing a No vote.

**Tradeoff:** No exclusion and Skip resurfacing are not active in this list; they apply to the future deck. The stored 15-Yes target is not presented as an achievable task with only three samples. There has been viewport/browser verification, not a comprehensive accessibility audit.

### 24. Plain CSS and system fonts

**Decision:** Use a small warm kitchen visual style, ordinary CSS, and no UI kit, icon package, remote font, or image service. Prettier is a development-only formatter.

**Why:** It gives the first slice a coherent phone-friendly presentation while keeping dependencies and asset requests small. Source remains directly editable as the visual design evolves.

**Tradeoff:** This is an initial design, not a final brand system. Illustrated plates are decorative samples, not photographs of the recipes. There is no reusable design-system package yet.

## Operations and verification

### 25. Local Compose with a same-origin web proxy

**Decision:** Three services: PostgreSQL, non-root API, nginx/static web. Expose web and database only on loopback. Keep database and Identity keys in named volumes. Use multi-stage builds so SDK/Node/build dependencies stay out of runtime images.

**Why:** This fulfills local packaging and makes the full stack reproducible without adding cloud infrastructure.

**Tradeoff:** The API needs time to migrate and seed after the database becomes healthy; the web can briefly start before API readiness. This configuration is Development-only and is not a deployment runbook.

### 26. Environment-managed random local credentials and a limited DB role

**Decision:** A setup script generates `.env`; no real credential is tracked. PostgreSQL's administrator password differs from the API's non-superuser login. The API can create its own tables to apply local migrations.

**Why:** It avoids a shared default password and removes unnecessary database superuser permissions from the app.

**Tradeoff:** Local `.env` remains plaintext, governed by workstation filesystem access. The app login still has schema modification rights for local convenience. Production needs a separate migration identity and secrets/key-management decisions.

### 27. Migrations separate from development data

**Decision:** Commit the generated EF migration and model snapshot. Apply migrations and sample seeding automatically only under Development with explicit flags. Bootstrap runs in a database transaction and leaves existing data intact.

**Why:** Schema history is reproducible; samples and credentials do not become permanent migration artifacts.

**Tradeoff:** Production migration orchestration is intentionally not implemented. Concurrent bootstrap, account rotation, and recovery from partially hand-edited local data are not supported workflows.

### 28. Lightweight observability

**Decision:** Structured JSON console logs, liveness/readiness endpoints, Problem Details, and no external telemetry service.

**Why:** Enough visibility to diagnose a local family app without operating a monitoring stack.

**Tradeoff:** Readiness checks database connectivity only. There are no dashboards, durable audit events, metrics, or distributed tracing. Development startup can emit expected EF first-migration/key-protection warnings; investigate runtime errors separately.

### 29. Two verification layers

**Decision:** Ordinary `dotnet test` includes pure rule tests and in-process API/Identity tests backed by isolated in-memory SQLite. A separate PowerShell smoke script exercises the running PostgreSQL Compose stack and restart persistence. Browser verification covers desktop and phone layouts.

**Why:** Cloud agents can run useful regression tests without Docker or secrets; real PostgreSQL verification checks the provider-specific migration, array mappings, and constraints that SQLite cannot prove.

**Tradeoff:** SQLite is a test substitute, never the application's runtime database. The browser checks are recorded manual automation, not a committed browser-test runner; no browser testing dependency is added yet. Normal first-time restore still needs a package source/cache.

### 30. Optional build-only trust for this workstation

**Decision:** Dockerfiles accept an optional BuildKit `build_ca` secret. This machine's existing Norton root certificate is supplied through an ignored local override to let Linux builders validate intercepted registry traffic.

**Why:** The first build failed with an incomplete certificate chain. Matching the workstation's established trust resolves it while keeping TLS verification enabled.

**Tradeoff:** This workstation needs the local override for uncached builds. The certificate and override are not tracked or copied into runtime images. Other developers without TLS inspection use plain Compose; they should not install this machine's certificate.

## Next architecture checkpoints

- **Milestone 2:** introduce a dedicated safe-fetch boundary and extraction service; add recipe provenance and preview confirmation semantics. SSRF, redirect/DNS rebinding, timeout, size, and parser tests are required before enabling import.
- **Milestone 3:** define deck query ordering and Skip resurfacing precisely, test No exclusion, and add target messaging.
- **Milestone 4:** add deterministic scoring and immutable finalized-plan snapshots; decide round-closing/concurrent-vote behavior as part of that lifecycle.
- **Deployment:** review HTTPS/trusted proxies, account administration, database/migration permissions, key protection, backups/restores, and container update strategy before exposing the app.
