# Architecture baseline

## Shape

Dinder is a small monorepo with a separate API and web client:

```text
src/
  Dinder.Api/          ASP.NET Core Web API
  Dinder.Web/          React + TypeScript client
tests/
  Dinder.Api.Tests/    API, domain, and integration tests
docs/
infra/                 deployment-only assets when needed
```

Use a modular monolith. Do not introduce microservices, message brokers, a separate BFF, or a vector database in Phase 1.

Milestone 1 implements this shape. Its concrete decisions and tradeoffs are recorded in [the decision inventory](milestone-1-decisions.md) and [ADR 0003](decisions/0003-milestone-1-foundation.md). See [current state](current-state.md) for implemented behavior and limitations.

## Responsibilities

- **React client:** responsive presentation, local interaction state, and calls to the API. It must not enforce security boundaries.
- **ASP.NET Core API:** identity, Chef/member authorization, household isolation, recipe import, voting, recommendation, and plan finalization.
- **PostgreSQL:** source of truth for operational data. EF Core migrations version schema changes.
- **Docker Compose:** brings up the API, web client, and PostgreSQL for local development and eventual home-server deployment.

## Data boundaries

`Household` is the tenancy boundary. A user belongs to a household through `HouseholdMember`, which carries the Chef or Member role. Recipes, planning rounds, votes, meal plans, and meal-plan items belong to exactly one household.

The API determines the caller's household membership and role for every request. It never accepts a household identifier from the client as sufficient authorization.

## Initial API design

Use conventional REST-style endpoints and DTOs. Keep API contracts versionable, but do not add formal versioning until an external client needs it. Expose a health endpoint and API documentation appropriate for local development.

The application starts as a single Chef-created household. Phase 1 uses ASP.NET Core Identity backed by PostgreSQL and cookie authentication. There is no public registration flow or external identity provider in the first milestone. This provides server-enforced identity without storing plaintext passwords and leaves room for invitations later.

## Recipe import boundary

The importer accepts a public recipe URL, fetches it server-side, extracts Schema.org `Recipe` JSON-LD when available, and returns a confirmation preview before persistence. Preserve canonical source attribution.

Treat URL import as a security boundary: only permit public HTTP(S) targets, resolve and reject private/link-local/loopback addresses, limit redirects, use short timeouts, bound response sizes, and do not fetch arbitrary non-HTML content.

## Scoring boundary

The recommendation service operates on persisted household votes and returns score explanations. Phase 1 is a deterministic function; scoring constants must be named and tested. The Chef remains the final editor of a plan.

## RAG runway, not current scope

After successful household use, PostgreSQL may enable `pgvector` for a constrained household-filtered retrieval feature. Do not add an LLM SDK, embeddings, vectors, or retrieval tables before that milestone is approved.
