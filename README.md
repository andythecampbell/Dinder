# Dinder

Dinder is a Chef-led family dinner-planning app: household members vote on recipes, and Dinder proposes a weekly five- or six-dinner menu for the Chef to finalize.

Milestone 1 is implemented: sign in as Chef or Member, access the development household, start a planning round, and persist Yes/No/Skip votes on sample recipes. Product scope and delivery guidance live in [`docs/`](docs/).

## Run locally

With Docker Compose and PowerShell 7, from the repository root:

```powershell
./infra/Initialize-Local.ps1
docker compose up --build -d
```

Open http://localhost:8080. Use `chef@dinder.local` or `member@dinder.local` with the corresponding randomly generated password in the ignored `.env` file. See [local setup](docs/local-development.md) for source development, certificate-inspecting workstations, migrations, and verification.

With the .NET 10 SDK, the normal test command is `dotnet test Dinder.slnx`. Tests run without Docker, a browser, or manually supplied secrets; initial package restoration needs network/cache access.

## Read first

- [Product brief and Phase 1 specification](docs/product-brief-and-phase-1.md)
- [Architecture](docs/architecture.md)
- [Implementation plan](docs/implementation-plan.md)
- [Current state](docs/current-state.md)
- [Agent guidance](AGENTS.md)
- [Milestone 1 architecture decisions and tradeoffs](docs/milestone-1-decisions.md)
- [Milestone 1 verification report](docs/milestone-1-report.md)
- [API reference](docs/api.md)

## Intended stack

- ASP.NET Core Web API
- React + TypeScript
- PostgreSQL + Entity Framework Core
- Docker Compose
- Home-server hosting through Cloudflare Tunnel

## Status

Milestone 1 complete. URL import, the voting deck, recommendations, and final meal plans remain subsequent milestones. This setup is local Development only; it has no public deployment configuration.
