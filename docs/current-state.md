# Current state

## Status

Milestone 1 is implemented and verified locally. The repository now contains an ASP.NET Core/.NET 10 API, React/TypeScript SPA, PostgreSQL schema and EF migration, Docker Compose packaging, and a deterministic 15-test suite. The local repository has been initialized on `main`; no remote or commit was created by this milestone.

## Implemented behavior

- ASP.NET Core Identity sign-in/sign-out, HttpOnly cookie sessions, antiforgery protection, and login throttling. Bootstrap users now participate in the configured lockout policy.
- Development-only bootstrap for `Our kitchen`, one Chef, one Member, and three original sample recipes. Random local passwords are generated into ignored `.env`; no real credentials are tracked.
- Authenticated household access and Chef-only planning-round creation.
- Persistent, independent Yes/No/Skip votes through explicit recipe details. Updates replace the current vote without duplicates.
- Server-side membership checks on every household endpoint, scoped reads/writes, and composite tenant-aware foreign keys.
- A partial unique index prevents multiple active rounds per household.
- Responsive sign-in, recipe list, and details/voting screens; desktop and 390 × 844 phone flows verified.
- Structured logs, health endpoints, local PostgreSQL non-superuser app credentials, and persisted database/Identity key volumes.
- [Local setup and verification](local-development.md), [API reference](api.md), [decision inventory](milestone-1-decisions.md), and [completion evidence](milestone-1-report.md).

## Verification

- `dotnet test Dinder.slnx --no-restore`: 15 passed, 0 failed, 0 skipped; tests require no Docker, external network, supplied secrets, or browser after restore.
- Release .NET build: zero warnings/errors. TypeScript/Vite production build and Prettier checks passed.
- Fresh PostgreSQL volume: initial migration and sample seed applied through Compose.
- Local HTTP smoke checks: authentication, authorization, CSRF, all three vote choices, vote updates, foreign-household rejection, and session/vote persistence after API restart passed.
- Browser: Chef sign-in/start/vote changes/reload, independent Member voting, and phone viewport verified. Screenshots are in `docs/screenshots/`.

## Known limitations and intentional deferrals

- This is the updated first-run brief's seeded-recipe slice. No URL importer, recipe deck, recommendation, final plan, notification, RAG, or public deployment exists.
- Household access is provisioned by the development seed. There is no household-creation wizard, public registration, invitation, account administration, or password-recovery flow.
- A Chef can start and close one active round at a time; closing a round allows the next week's round to start. No/Skip deck rules and 15-Yes progress messaging are Milestone 3. No-voted recipes remain available in the explicit recipe list/detail view for editing.
- The UI selects the first household membership; multi-household switching is not implemented. Another member's round-start requires Refresh/reload on an already-open screen.
- Current votes are stored across rounds; intra-round vote changes are not an append-only event history. Concurrent updates use last-write-wins, and simultaneous first writes may require a refresh/retry after 409.
- SQLite API tests exercise HTTP, Identity, and relational behavior but do not prove PostgreSQL migrations. The separate Compose smoke check covers the real provider; no committed browser test runner exists yet.
- Local migration privileges, unencrypted data-protection keys, Development HTTP cookies, and bootstrap accounts are not a production security/operations setup. Backup/restore and Cloudflare remain Milestone 5 work.
- This machine's Norton TLS inspection required an optional ignored build-certificate override. See local setup for reproducing it without disabling TLS verification. Default Compose remains suitable for machines without inspection.

## Approved decisions

- One private household in Phase 1, with future multi-household boundaries.
- Chef-only recipe importing, planning-round control, and plan finalization.
- Independent voting during a weekly round.
- Vote choices: Yes, No, and Skip.
- No hides a recipe from the voting member and lowers its household score; it is not a global veto.
- Skip is neutral and may resurface after unseen eligible recipes are exhausted.
- Initial target: 15 Yes votes per member.
- The Chef receives an in-app proposed five- or six-dinner menu and retains final editorial control.
- Recipe entry begins with Chef-provided public URLs and structured-data extraction.
- Intended implementation: ASP.NET Core API, React/TypeScript client, PostgreSQL, Docker Compose, home server, and Cloudflare Tunnel.
- Initial authentication: ASP.NET Core Identity backed by PostgreSQL with cookie authentication; no external provider or public registration.

## Deferred decisions

- Invitation flow and any future external identity provider.
- Final visual design and brand identity.
- Score weights beyond the initial transparent baseline.
- Email-notification provider and timing.
- Deployment runbook specifics, Cloudflare configuration, and domain name.
- RAG design and provider selection.

## Next planned milestone

Milestone 2 in `docs/implementation-plan.md`: Chef-only safe recipe URL fetch, structured extraction, preview, source attribution, and confirmed persistence. This milestone has not been implemented as part of the current task.
