# Dinder agent guidance

## Source of truth

Read `docs/product-brief-and-phase-1.md`, `docs/architecture.md`, `docs/implementation-plan.md`, and `docs/current-state.md` before proposing or making product changes. The product brief wins when documents conflict.

## Project intent

Dinder is a private family dinner-planning web app, designed with a future multi-household/open-source product in mind. Phase 1 must make the Chef-led recipe import, independent voting, recommendation, and final weekly-plan workflow work before adding AI, RAG, notifications, or broad recipe crawling.

## Architecture boundaries

- API: ASP.NET Core Web API; client: React + TypeScript; data: PostgreSQL with Entity Framework Core migrations.
- The API enforces authentication, authorization, roles, and household isolation. Never rely on client-side checks for access control.
- Every household-owned record must be scoped by `household_id`; queries and mutations must verify membership server-side.
- Keep the recommendation algorithm deterministic and explainable. Do not add LLMs, embeddings, or hidden scoring heuristics in Phase 1.
- URL import is Chef-only and must prefer structured recipe data. Treat submitted URLs as untrusted input: block private/internal destinations, bound redirects, timeouts, and response sizes.
- Use Docker Compose for local development and home-server deployment. Do not change hosting, authentication providers, or introduce managed cloud services without approval.

## Engineering expectations

- Make the smallest coherent change that meets the stated acceptance criteria.
- Prefer conventional .NET, React, TypeScript, PostgreSQL, and Docker patterns over novel abstractions.
- Favor clear, cohesive code and deliberate extensibility. Build explicit extension points for roadmap-backed changes: recipe importing, recommendation policies, notifications, identity/invitations, and future RAG.
- Keep implementations simple within those boundaries. Introduce generic repositories, service layers, factories, event buses, feature flags, or interfaces only when a current requirement or a named next milestone needs them; do not add general-purpose machinery for an unspecified hypothetical use.
- Prefer stable API contracts, modular domain boundaries, and named policies/services around expected variation. Examples include external URL fetching, recipe extraction, deterministic recommendation scoring, clock/randomness, and notification delivery.
- Prefer explicit names, small focused methods/components, typed contracts, predictable error handling, and comments that explain non-obvious intent rather than restating code.
- Keep business rules in testable application/domain code rather than burying them in controllers, EF queries, or React components.
- Keep API contracts typed and validate external input at the API boundary.
- Add or update tests for behavior changes. Do not claim a check passed unless it was run successfully.
- Start with meaningful behavior-focused unit tests. They must run deterministically through the normal test command without Docker, external network access, a real browser, or manually supplied secrets, so cloud agents can run them.
- Keep unit tests fast and independent. Add integration tests for important API/auth/database boundaries, but do not make them the only test suite or require Docker for the ordinary test command.
- Test the rules that could harm the product if wrong: household isolation, Chef authorization, voting behavior, deterministic scoring, final-plan immutability, and URL-import safety.
- Do not set a coverage percentage target yet; prioritize tests that protect decisions and regression-prone behavior. Revisit coverage only after the suite has a meaningful baseline.
- Update `docs/current-state.md` when a milestone changes what exists, adds a known limitation, or defers a decision.
- Add a short ADR in `docs/decisions/` for a durable, consequential decision.
- Do not modify secrets, production configuration, external accounts, DNS, Cloudflare, or deployment infrastructure without explicit approval.

## Stop and ask for approval

Pause before changing the selected technology stack, database conventions, authentication approach, public exposure model, data ownership model, or any material dependency. Pause if product behavior is ambiguous and the choice would affect the user experience or data model.

## Completion report

At the end of a task, report: implemented behavior, files changed, tests/checks actually run and their results, notable decisions, known limitations, and the most useful manual verification steps.
