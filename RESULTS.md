# Benchmark Results — .NET 8 → 10 migration, three ways

Same model in every run — **Claude Sonnet 5 (1M context), high thinking.** This is a
**tooling + process** comparison, not a model comparison. Each run started from the identical
baseline commit; the three migrations live on `bench/copilot`, `bench/claude-plain`, and
`bench/claude-guarded`.

## Baseline (before any migration)

```
branch     : master
dotnet SDK : 8.0.423
TFMs       : net8.0
LOC (.cs)  : 1522
build      : PASS (warnings: 0, errors: 0)
tests      : passed 12 / failed 0 / total 12
```

Target: **.NET 10** — all projects → net10.0, packages → 10.x, global.json bumped, build +
12 tests green, no behavior change.

---

## Head-to-head

| | A — Copilot modernize-dotnet | B — plain Claude Code | C — Claude Code + guardrails |
|---|---|---|---|
| Model | Sonnet 5, high | Sonnet 5, high | Sonnet 5, high |
| Interventions | none (autonomous) | none (autonomous) | none (autonomous) |
| Wall time | 6m 13s | **5m 29s** (fastest) | 6m 31s + ~2m setup |
| Errors / Tests | 0 / 12 ✅ | 0 / 12 ✅ | 0 / 12 ✅ |
| FastEndpoints break | fixed | fixed | fixed (reflection-verified) |
| **Warnings delivered** | **0** | **6** | **0** |
| **Security CVE-2025-6965** | **fixed** | **deferred** | **fixed** |
| global.json bumped | ✅ | ✅ | ✅ |
| Hooks fired | n/a | n/a | investigation-gate ×1 |

**Takeaway:** all three produced a correct, green, fully-autonomous migration and all three hit
and fixed the same FastEndpoints breaking change — plain Claude matched Copilot on core
correctness. The difference was **cleanliness of what got handed back**: Copilot and the guarded
run both delivered production-clean (0 warnings, CVE patched), while the bare agent shipped
technically-green but with 6 warnings and a knowingly-deferred vulnerability. On the same model,
the guardrails brought Claude Code up to the production-clean bar the bare agent didn't reach on
its own — while the bare agent was fastest on the clock. Honest speed-vs-thoroughness tradeoff.

**Caveats (stated plainly for the post):** n=1 per configuration — the differences are real but
not proven systematic from one migration; the deltas are small; the bare agent was the fastest;
and the guarded run carried real setup friction (the MCP DLL fix below).

---

## RUN A — GitHub Copilot modernize-dotnet   (branch: bench/copilot)

```
Model used   : Claude Sonnet 5 (1M context), high thinking — via Copilot's model picker
Interventions: none — autonomous through assess → plan → execute
Wall time    : 6m 13s
End state    : net10.0 ×5 | build 0 warnings / 0 errors | tests 12/12
global.json  : YES → 10.0.102
```
**Changed:** 5 projects → net10.0; global.json → 10.0.102; dotnet-tools.json → dotnet-ef 10.0.10;
EF Core/.Sqlite/.Design → 10.0.10; DI.Abstractions → 10.0.10; Mvc.Testing → 10.0.10;
Test.Sdk → 18.8.1; xunit → 2.9.3 (runner.visualstudio kept at 2.8.2); FastEndpoints/.Swagger → 8.2.0.
Fixed the FastEndpoints `SendAsync` → `Send.OkAsync` / `Send.ResponseAsync` break (5 compile errors across 5 endpoints).
**Proactively added `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12** to clear the transitive NU1903 advisory.
**Best:** caught the breaking change AND patched the security vuln — production-clean.
**Cost:** requires a paid Copilot subscription; IDE-bound.
**Screenshot:** benchmark-screenshots/run-a-copilot/

## RUN B — Claude Code, plain (no guardrails)   (branch: bench/claude-plain)

```
Model used   : Sonnet 5, high thinking (/model sonnet) — no CLAUDE.md / hooks / MCP
Interventions: none — autonomous, single prompt
Wall time    : 5m 29s (fastest)
End state    : net10.0 ×5 | build 6 warnings / 0 errors | tests 12/12
global.json  : YES → 10.0.102
```
**Changed:** 5 projects → net10.0; global.json → 10.0.102; EF Core/.Sqlite/.Design/DI.Abstractions/
Mvc.Testing → 10.0.10; Test.Sdk → 18.8.1; xunit → 2.9.3; xunit.runner.visualstudio → 3.1.5;
FastEndpoints/.Swagger → 8.2.0. Fixed the FastEndpoints break (5 compile errors).
**Best:** matched Copilot on correctness — clean green migration, fastest wall time.
**Lost:** left **6 build warnings**; **recognized the NU1903 / SQLite CVE but deferred it** as
"out of scope for a migration" — shipped a green build carrying a known vulnerability.
**Screenshot:** benchmark-screenshots/run-b-claude-plain/

## RUN C — Claude Code + twelve-layer guardrails   (branch: bench/claude-guarded)

```
Model used   : Sonnet 5, high thinking (/model sonnet) — tiering OFF, identical to Run B
Interventions: none — fully autonomous
Wall time    : 6m 31s
Setup time   : ~2m — MCP server wouldn't start under the repo's global.json pin; had to switch
               .mcp.json from `dotnet run --project` to the built DLL (dotnet run resolved SDK
               8.0.423 from cwd and couldn't build the net10 server). Honest guardrail cost.
Hooks fired  : YES — investigation-gate hook fired once
Hallucinated APIs : none — verified FastEndpoints signatures by reflection on the installed assembly
End state    : net10.0 ×5 | build 0 warnings / 0 errors | tests 12/12 | LOC 1522
global.json  : YES → 10.0.102
```
**Changed:** 5 projects → net10.0, **LangVersion 14.0**; global.json → 10.0.102; dotnet-tools.json →
dotnet-ef 10.0.10; EF Core/.Sqlite/.Design → 10.0.10; DI.Abstractions → 10.0.10; Mvc.Testing → 10.0.10;
Test.Sdk → 18.8.1; xunit → 2.9.3 (kept v2 — v3 is a framework migration, out of scope);
FastEndpoints/.Swagger → 8.2.0. Fixed the FastEndpoints break — **verified exact replacement
signatures via reflection rather than guessing**.
**Fixed CVE-2025-6965:** EF Core 10 Sqlite pulled `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (published
high-severity CVE); pinned to 2.1.12. Its stated reasoning: "the baseline had 0 warnings, so I
didn't want to hand back a build with a new vulnerability."
**Best:** matched Copilot's production-clean bar (0 warnings, CVE patched) that the bare agent
missed; verified the API change against the real assembly instead of guessing.
**Cost:** guardrail setup friction (the MCP DLL fix above).
**Screenshot:** benchmark-screenshots/run-c-claude-guarded/

---

## Ground rules honored
- Timeboxed; first attempt counted; no re-runs for prettier numbers.
- Copilot's real wins conceded (autonomous, caught the break, patched the vuln, production-clean).
- All three runs fully autonomous; all three branches pushed — public and reproducible from the baseline commit.
- Same model + thinking level across all three runs — the only variables were the tool and the guardrails.
