# Dinder — Product Brief and Phase 1 Specification

## Purpose

Dinder is a shared household dinner-planning app. Family members independently make quick choices about recipes; Dinder turns those votes into a recommended weekly dinner menu for the household's designated Chef to confirm.

The immediate goal is to solve one household's recurring "what are we having for dinner?" problem in a way that is fast, collaborative, and fun. The project is also a deliberate learning vehicle for agentic coding, React, PostgreSQL, self-hosting, and—soon after the core workflow proves useful—retrieval-augmented generation (RAG).

Phase 1 is private and deployed for a single household, but the domain model and authorization boundaries should allow multiple households later.

## Product principles

- Make choosing dinner feel lighter than discussing it in a group chat.
- Favor a useful, transparent workflow over premature intelligence.
- Keep recipe ownership and attribution clear: imported recipes remain linked to their original source.
- Build a safe baseline for a family app without overbuilding enterprise infrastructure.
- Preserve enough structured history to support future personalization and RAG.
- Let autonomous agents implement bounded milestones, while humans retain product and architecture decisions.

## Phase 1 users and roles

### Chef

The Chef is the household member who owns the weekly planning process. In Phase 1, the Chef can:

- import recipes from URLs;
- start and close a weekly planning round;
- see the recommendation and vote summary;
- add, remove, and reorder dinners before finalizing the week.

The Chef also votes on recipes. The finalization workflow is the Chef's practical veto; a separate Chef-veto vote type is intentionally deferred.

### Household member

Household members can:

- sign in to their household;
- independently vote on recipes while a planning round is active;
- see the finalized weekly dinner plan.

## Core experience

1. The Chef imports recipes into the household recipe collection by pasting recipe page URLs.
2. The Chef starts a weekly planning round.
3. Each household member opens a recipe deck and chooses **Yes**, **No**, or **Skip** for recipes independently throughout the week.
4. Dinder ranks recipes from the household's votes and proposes a five- or six-dinner menu to the Chef.
5. The Chef reviews the vote summary, edits and reorders the proposed menu, then finalizes the plan.
6. Every household member can view the final weekly plan in the app.

## Voting rules

- **Yes:** positive vote; contributes to the recipe's household score.
- **No:** negative vote; lowers the recipe's household score and prevents the recipe appearing again in that member's deck for the active round.
- **Skip:** no positive or negative signal; the recipe may reappear after the member has exhausted unseen eligible recipes.
- Each member has an initial target of 15 Yes votes per active planning round. The target is a configurable application setting later, but may be a fixed constant in the first implementation.
- A member can change an existing vote in an explicit recipe-detail view. The deck itself should not accidentally re-show a No vote.
- A No vote is not a global veto. The Chef sees vote counts when deciding whether to include a highly ranked recipe.

## Recipe import

Phase 1 recipe entry is Chef-only and URL-first. The importer should fetch a public recipe URL and attempt to extract structured recipe data, preferring Schema.org `Recipe` JSON-LD. On success, it presents a preview for Chef confirmation.

Persist, where available:

- title;
- canonical source URL and source/site name;
- image URL;
- prep, cook, and total time;
- servings/yield;
- ingredients and instructions;
- categories, cuisines, and relevant tags;
- import timestamp and extraction status.

Recipes must visibly retain source attribution and link back to the canonical URL. The app should not imply ownership of source material. If structured data cannot be extracted, show a clear failure state and allow the Chef to try another URL. Manual recipe entry, broad crawling, and per-site adapters are deferred.

For public-minded implementation, the URL fetcher must reject private/internal addresses, set tight timeouts and response-size limits, and limit redirects. These protections are required even though the initial deployment is private.

## Recommendation and weekly plan

The first recommendation algorithm is deliberately explainable:

- calculate a score from Yes and No votes;
- choose the highest-ranked eligible candidates;
- propose five or six dinners;
- show the Chef why each candidate ranked as it did (at minimum, Yes and No counts).

Exact weights may start as a named application constant, for example `Yes = +1`, `No = -1`, and `Skip = 0`. Do not add machine learning, variety heuristics, cost optimization, or per-person preference weighting in Phase 1.

The Chef can remove a proposal, select another ranked recipe, add an eligible recipe manually, reorder days, and finalize the plan. The finalized plan is a snapshot: later voting must not silently alter it.

## Scope

### In scope

- one household with an architecture that supports many;
- authentication and server-enforced household membership;
- Chef role;
- recipe URL import and confirmation preview;
- household recipe collection;
- active weekly planning rounds;
- independent Yes / No / Skip voting;
- score-based menu proposal;
- Chef review and finalization of five or six dinners;
- finalized plan view;
- responsive web UI suitable for phones;
- local and home-server deployment documentation.

### Explicitly deferred

- email, push, or SMS notifications (targeted for Phase 1.5);
- individual dietary profiles and recipe alternatives;
- grocery lists, pantry tracking, budgets, nutrition, and calendar sync;
- public self-service signup and invitation flows;
- broad web scraping, recipe discovery, and source-specific importers;
- native mobile apps;
- AI-generated menus and chat;
- RAG features.

## Technical direction

- **API:** ASP.NET Core Web API.
- **Web client:** React and TypeScript, as a separate SPA.
- **Database:** PostgreSQL.
- **Data access:** Entity Framework Core with migrations.
- **Local and production packaging:** Docker Compose.
- **Hosting:** home server, exposed through Cloudflare Tunnel.
- **Observability:** structured application logs and health endpoints; keep this lightweight in Phase 1.

PostgreSQL is selected not because Phase 1 needs a vector database, but because it is a strong relational fit and supports a later `pgvector` extension. The API remains the sole authority for authentication, authorization, and household data access; the client must never be trusted to enforce these boundaries.

Use environment-managed secrets, HTTPS via Cloudflare, least-privilege database credentials, and a documented database backup/restore procedure. Do not add external managed services unless the operational cost of self-hosting becomes a demonstrated problem.

## Initial data model

- **User:** authenticated person.
- **Household:** data boundary for recipes, rounds, and plans.
- **HouseholdMember:** maps users to households and records `Chef` or `Member` role.
- **Recipe:** normalized household recipe record plus source and extraction metadata.
- **PlanningRound:** a household's weekly voting period and target Yes count.
- **Vote:** a member's Yes, No, or Skip choice for a recipe in a planning round.
- **MealPlan:** the Chef-confirmed plan associated with a planning round.
- **MealPlanItem:** an ordered dinner entry in the finalized plan.

Every household-owned record must carry a household identifier. Queries and mutations must verify membership server-side.

## Acceptance criteria for the first vertical slice

1. A Chef can sign in and create or access the household.
2. A Chef can submit a supported public recipe URL, inspect extracted metadata and image, and confirm its addition to the household collection.
3. The Chef can start a planning round.
4. A member can independently work through a recipe deck and record Yes, No, and Skip choices.
5. A No recipe does not reappear in that member's active-round deck; a Skip recipe may return after unseen recipes are exhausted.
6. Votes persist and are scoped to the active household and planning round.
7. The Chef can see a ranked, explainable proposed menu of five or six recipes.
8. The Chef can edit, reorder, and finalize the menu.
9. Household members can view the finalized menu.
10. The app works at a practical phone viewport and runs from Docker Compose locally.

## Phase 1.5: notifications

After the core loop succeeds in real household use, add email reminders:

- remind members when an active round needs their votes;
- notify the Chef when the round has enough participation or reaches its deadline.

Keep notification delivery behind an interface so a future push-notification implementation does not change planning logic.

## RAG path

RAG should follow, not replace, two successful real planning cycles. Phase 1 creates the data foundation by retaining structured recipe metadata, vote history, finalized plans, and future optional meal feedback.

The first RAG feature should be narrow and explainable, for example: “Suggest three recipes our household is likely to enjoy based on past votes and meal feedback.” Use PostgreSQL with `pgvector`, strict household filtering, and visible source evidence. Do not introduce embeddings or an LLM into the core scoring path until the deterministic baseline is understood.

## Autonomous-agent workflow

Use milestone autonomy rather than atomized PR automation or an unbounded “build the app” request.

Each agent task must include:

- the relevant section of this specification;
- explicit scope and non-goals;
- acceptance criteria;
- permitted files and dependencies;
- stop conditions for consequential decisions;
- expected evidence: tests run, screenshots for UI work, migration notes, decisions made, and known limitations.

The agent may implement routine code, tests, styles, documentation, and bounded refactors. It must pause for approval before changing the architecture, authentication approach, deployment approach, data boundaries, database conventions, or adding material dependencies.

For each milestone, the human reviews a coherent outcome rather than reconstructing intent from numerous tiny commits. Keep a current-state document and short decision records so later code review answers: “Why is this here, and was it intentional?”

## Suggested first agent milestone

Create the repository foundation and the vertical slice through recipe import and voting. Include the ASP.NET Core API, React client, PostgreSQL Docker Compose setup, migrations, a household/Chef authorization boundary, structured recipe URL import preview, planning-round creation, and persistent Yes/No/Skip voting. Do not implement recommendation generation, final meal plans, notifications, RAG, or deployment changes beyond local Docker Compose documentation.
