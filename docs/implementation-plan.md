# Phase 1 implementation plan

Each milestone should leave the application buildable, tested proportionally to its risk, and documented in `docs/current-state.md`.

## Milestone 1 — Foundation and a vertical slice

Create the repository structure, ASP.NET Core API, React client, PostgreSQL Docker Compose setup, and test projects. Implement a locally usable single-household foundation: identity/authentication, Chef role, health checks, EF Core migrations, and a responsive shell.

Then implement the smallest end-to-end product slice:

- Chef can create/access the household.
- Chef can start a planning round.
- A signed-in household member can record a persistent Yes, No, or Skip vote for a seeded development recipe.
- The API enforces household isolation and Chef-only actions.

Out of scope: URL import, recommendation, finalized plans, notifications, RAG, Cloudflare deployment, and public sign-up.

**Exit criteria:** a fresh local Docker Compose environment can run the app; the vertical slice works in a browser; API tests cover authorization and vote persistence; setup and verification steps are documented.

## Milestone 2 — Recipe URL import

Implement Chef-only URL submission, safe fetching, JSON-LD extraction, confirmation preview, persistence, source attribution, and import failure states. Include tests for extraction and URL safety boundaries.

**Exit criteria:** Chef imports a real supported public recipe URL, sees preview data and image, confirms it, and sees it in the household collection.

## Milestone 3 — Voting deck

Replace the seeded-recipe interaction with the responsive recipe deck. Implement Yes/No/Skip rules, 15-Yes target messaging, No exclusion for the voter, and controlled Skip resurfacing after unseen eligible recipes are exhausted.

**Exit criteria:** multiple members can vote independently in one active round, and data persists accurately.

## Milestone 4 — Recommendation and final plan

Implement deterministic, explainable household scoring. Deliver a Chef-facing five- or six-dinner proposal with vote summaries. Support Chef edits, reordering, and finalization; expose the final plan to all household members.

**Exit criteria:** one household completes the complete weekly workflow end to end.

## Milestone 5 — Real-world hardening

Run at least two actual planning cycles. Fix usability and reliability issues, document backup/restore and home-server deployment, and only then decide whether to add Phase 1.5 email notifications.

## Phase 2 candidate — RAG

After vote and optional meal-feedback history exists, prototype household-filtered retrieval with `pgvector`. Start with a cited, explainable suggestion feature; do not replace deterministic weekly ranking until it earns that trust.
