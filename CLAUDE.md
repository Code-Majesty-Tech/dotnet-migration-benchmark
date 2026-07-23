# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project Overview

{One paragraph. What the system is, who uses it, what the money path is.
No marketing language — the agent needs orientation, not persuasion.}

## Tech Stack

.NET 10 (LTS) | C# 14 | Aspire | EF Core 10 + PostgreSQL | xUnit + Testcontainers

Pin versions here and update them when you upgrade. The agent's training
prior will happily generate .NET 6 idioms unless told otherwise.

## Commands

```bash
dotnet build                          # must pass before any task is "done"
dotnet test                           # full suite
dotnet test --filter "Category=Unit"  # fast loop
dotnet run --project src/AppHost      # Aspire — starts the full local topology
```

## Solution Structure

```
src/
├── AppHost/          # Aspire orchestration — service topology lives HERE, nowhere else
├── Api/              # FastEndpoints, vertical slice: Features/{Area}/{Feature}/
├── Domain/           # Entities, invariants. No EF, no HTTP, no exceptions to this
├── Infrastructure/   # EF Core, migrations, external services
└── Tests/            # Mirrors src/ structure one-to-one
```

## Investigation Before Implementation (MANDATORY)

Before writing or modifying ANY code:

1. Read the relevant feature folder end to end — endpoint, handler, entity, tests.
2. Verify every symbol you plan to call actually exists, with the signature
   you think it has. Do not implement from memory of "how .NET usually works."
3. If existing docs or patterns contradict the request, STOP and ask.

This project has conventions that differ from framework defaults. Training-data
instincts are wrong here more often than they are right.

## Clarification Policy

Ask clarifying questions BEFORE planning or executing when anything is
ambiguous. Keep asking until the scope is unambiguous. Too many questions
beats one wrong assumption.

Exception: messages containing "automation run" skip clarification and
execute directly.

## Git Policy

- NEVER commit or push directly. The human runs the commit command.
- Branch names: feature/{ticket}-{slug}. No direct commits to main.
- Never rewrite history. Never force-push. Ever.

## Secrets Policy

- Never read, print, or copy values from .env, appsettings.*.json
  connection strings, or user-secrets.
- Reference secrets by NAME in code and docs, never by value.

## Key Patterns (enforced in review)

- EF Core: AsNoTracking by default. Tracking queries only in handlers that
  mutate, and the mutation must be in the same handler.
- Endpoints: one FastEndpoints class per file, suffix "Endpoint".
  Request/Response records, suffix "Request"/"Response". Entities never
  cross the API boundary.
- Async: suffix "Async" on public methods only. No async void. No .Result.
- Migrations: generated names only — never hand-edit a migration file;
  add a new one.
- Nullability: nullable is enabled solution-wide. No #nullable disable. No !
  unless a comment justifies it on the same line.

## Definition of Done

A task is complete when: dotnet build passes, dotnet test passes, new code
has tests matching the existing test style, and no TODO you introduced
remains. "It compiles" is not done.
