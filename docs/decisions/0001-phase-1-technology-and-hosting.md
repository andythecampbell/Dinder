# ADR 0001: Phase 1 technology and hosting

**Status:** Accepted

## Decision

Build Dinder as an ASP.NET Core Web API with a React + TypeScript client and PostgreSQL database. Package local development and home-server deployment with Docker Compose. Expose the private service through Cloudflare Tunnel when deployment is approved.

## Why

This keeps the backend in the maintainer's strongest ecosystem while making React a deliberate learning area. PostgreSQL is a conventional relational fit now and provides a future path to `pgvector` without requiring an additional database. Home-server deployment is low-cost and aligned with the intended private household use.

## Consequences

The project owns database backup, updates, Docker operations, and secure tunnel configuration. Vercel, Supabase, and other managed services remain viable future alternatives but are not dependencies in Phase 1.
