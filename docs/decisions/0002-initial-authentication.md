# ADR 0002: Initial authentication

**Status:** Accepted

## Decision

Use ASP.NET Core Identity backed by PostgreSQL with cookie authentication for the initial private-household application. Do not add public registration, invitations, or an external identity provider in Milestone 1.

## Why

It is a conventional .NET solution that gives the API an enforceable identity boundary without relying on an external account or service. It also keeps the first autonomous build from being blocked by an unresolved authentication choice.

## Consequences

The first app will need a documented development/bootstrap path for the Chef account. A future invitation or external-login flow requires an explicit decision and migration plan.
