# SaaS Platform API — .NET 8 → 10 migration benchmark

> **This repo is the fixed baseline for a reproducible migration benchmark.** The `master`
> branch is a green .NET 8 solution; three `bench/*` branches each migrate it to .NET 10 a
> different way — GitHub Copilot modernize-dotnet, plain Claude Code, and Claude Code under
> the twelve-layer guardrail system. Every run starts from the identical baseline commit, so
> the results are comparable and anyone can reproduce them. Method and numbers:
> [the write-up](https://codemajesty.tech/blog/github-copilot-modernize-dotnet-vs-claude-code-honest-take).
> Run `./bench-baseline.sh` on any branch to print its build/test/LOC state; `RESULTS.md`
> holds the per-run notes.

A small but production-shaped multi-tenant SaaS backend built on **.NET 8**. It models the
core of a subscription business — **Organizations**, their **Users**, and a per-organization
**Subscription** — with real domain rules, EF Core persistence, and a vertical-slice HTTP API.

This repository is intentionally a clean, idiomatic **.NET 8** baseline (net8.0 target,
.NET-8-generation package versions, C# 12). It exists to be migrated to .NET 10 later, so it
uses ordinary .NET 8 patterns with no artificially inserted legacy APIs.

## Architecture

A layered / clean-architecture solution:

| Project | Responsibility | Key dependencies |
|---------|----------------|------------------|
| **Domain** | Entities, enums, and domain invariants. No external dependencies. | — |
| **Application** | Service layer + DTO contracts + the `IAppDbContext` abstraction. Orchestrates the domain. | Domain, EF Core (abstractions) |
| **Infrastructure** | EF Core 8 `DbContext`, entity configurations, the checked-in migration, and DI wiring. | Application, Domain, EF Core 8 (SQLite) |
| **Api** | ASP.NET Core Web API using **FastEndpoints** in a vertical-slice layout (`Features/{Area}/{Feature}/`). | Application, Infrastructure, FastEndpoints |
| **Application.Tests** | xUnit unit tests (pure domain) + integration tests (full API over SQLite in-memory). | Api, Domain |

The dependency direction points inward: `Api → {Application, Infrastructure} → Domain`.
The Application layer talks to persistence only through `IAppDbContext`, which
Infrastructure's `AppDbContext` implements.

## Domain model

- **Organization** — aggregate root (tenant). Owns its members and exactly one subscription.
  Creating one seeds an `Owner` user and a `Free` subscription. Enforces seat limits and
  unique member emails when adding members.
- **User** — a member of an organization with a role (`Member`, `Admin`, `Owner`).
- **Subscription** — one-to-one with an organization. Plan (`Free`/`Starter`/`Pro`/`Enterprise`)
  determines the seat limit; tracks status (`Trialing`/`Active`/`PastDue`/`Canceled`) and the
  current billing period.

## HTTP endpoints

All routes are prefixed with `/api`.

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/organizations` | Create an organization (seeds owner + Free subscription). |
| `GET`  | `/organizations` | List organizations. |
| `GET`  | `/organizations/{id}` | Get one organization. |
| `POST` | `/organizations/{organizationId}/members` | Add a member (enforces seat limit). |
| `PUT`  | `/organizations/{organizationId}/subscription/plan` | Change the subscription plan. |

Domain and application errors are mapped to RFC 7807 `ProblemDetails` via an
`IExceptionHandler`: `NotFoundException → 404`, `ConflictException → 409`,
`DomainException → 400`.

Swagger UI (via `FastEndpoints.Swagger`) is available at `/swagger` when running.

## Getting started

Prerequisites: the **.NET 8 SDK**. `global.json` pins the SDK to the 8.0.4xx band so the
build behaves like .NET 8 even if newer SDKs are installed.

```bash
# restore & build
dotnet build

# run the tests (unit + integration)
dotnet test

# run the API (applies EF migrations to a local SQLite file on startup)
dotnet run --project src/Api
```

The database is SQLite (`saasplatform.db`, created from the checked-in EF migration on first
run). Integration tests use a private SQLite **in-memory** database instead.

## Working with EF Core migrations

The EF tools are pinned as a local tool (`.config/dotnet-tools.json`). A design-time factory
(`AppDbContextFactory`) lets the tools run without booting the API host.

```bash
dotnet tool restore
dotnet ef migrations add <Name> \
  --project src/Infrastructure --startup-project src/Infrastructure \
  --output-dir Persistence/Migrations
```
