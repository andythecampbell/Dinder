# ADR 0003: Milestone 1 foundation and boundaries

**Status:** Implemented within the approved stack and initial authentication approach.

## Context

The updated implementation plan and first-run brief define Milestone 1 as seeded-recipe voting. They refine the product brief's older suggested milestone, which also mentioned URL import. The product's eventual behavior is unchanged; delivery is split into smaller reviewable steps. ADRs 0001 and 0002 remain the authorities for technology, hosting intent, and authentication.

## Decisions

Use one .NET 10 API with domain rules, a small EF-backed planning service, and REST endpoints; use a separate React/TypeScript SPA. Domain tables carry `household_id`, and composite foreign keys enforce tenant consistency. Household roles are membership data, not global Identity roles. PostgreSQL is authoritative; one active round per household and one current vote per member/recipe/round are database constraints.

Use same-origin cookies and antiforgery protection through an nginx reverse proxy locally. Bootstrap sample users, a household, and original recipes only in explicitly enabled Development mode. Apply migrations automatically only in that local mode. Keep PostgreSQL and data-protection keys in named volumes and use a non-superuser runtime database login.

Test pure business rules and real HTTP/Identity behavior without Docker, using SQLite only as the test database. Verify PostgreSQL migrations and persistence separately against Compose. Do not mistake SQLite test results for PostgreSQL verification.

## Why and consequences

This keeps the first slice understandable and testable without introducing a repository abstraction, mediator, state-management library, external identity service, or schema for deferred features. The tradeoffs are explicit query scoping, manually mirrored TypeScript contracts, a development-only account bootstrap, and a separate provider-specific verification step. Those are acceptable at this size and have identified extension points.

The complete decision-by-decision rationale, alternatives, and review path are in [Milestone 1 architecture decisions](../milestone-1-decisions.md). Operational instructions and limitations live in [local development](../local-development.md) and [current state](../current-state.md).
