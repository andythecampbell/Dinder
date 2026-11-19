# First autonomous run: Milestone 1

Use this as the task brief for an autonomous coding run.

## Mission

Implement Milestone 1 from `docs/implementation-plan.md`: establish the Dinder monorepo and ship the locally usable seeded-recipe voting vertical slice.

## Required reading

Read `AGENTS.md` and all documents named in its Source of truth section before making changes.

## Acceptance criteria

- The repository contains the API, web client, API test project, Docker Compose setup, and clear local setup instructions.
- A fresh local environment can start the API, web client, and PostgreSQL.
- An authenticated Chef can create or access the Phase 1 household and start a planning round.
- An authenticated member can cast one persistent Yes, No, or Skip vote for a seeded development recipe in that active round.
- Household isolation and Chef-only actions are enforced by the API and covered by tests.
- The normal unit-test command runs deterministically in a cloud environment without Docker, external network access, manually supplied secrets, or a real browser.
- The client is usable at a phone-sized viewport.
- `docs/current-state.md` accurately records what exists and any intentional limitation.

## Constraints

- Use the approved stack and modular-monolith structure.
- Use conventional, maintainable defaults rather than custom frameworks or speculative abstractions.
- Seed development-only recipes in a documented way; do not implement URL import yet.
- Do not add notifications, recommendation logic, plans, RAG, a vector database, Cloudflare configuration, public signup, external auth providers, or external accounts.
- Do not commit secrets or add production deployment credentials.

## Required stop conditions

Pause and request approval before selecting a non-local authentication provider, adding a major runtime dependency, changing the documented directory structure, or making a product choice not resolved in the specification.

## Evidence required at completion

- Short implementation report with changed files and decisions.
- Exact commands run and their successful or failed results.
- A brief test inventory explaining which behavior is covered by cloud-runnable unit tests and which checks require integration infrastructure.
- Manual browser verification steps and screenshots of the primary flow.
- Any known limitations or follow-up work added to `docs/current-state.md`.
