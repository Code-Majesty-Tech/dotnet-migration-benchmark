# Benchmark Results — .NET 8 → 10 migration, three ways

Fill this during/after each run. Hand the whole file back to write the post.
Protocol: `CodeMajestyTechWebsite/docs/research/ai_content/POST_11_BENCHMARK_PROTOCOL.md`.

## Baseline (before any migration)

_Run `./bench-baseline.sh` on the starting commit and paste it here._

```
branch     : master (ffd826d-ish — record actual)
dotnet SDK : 8.0.423
TFMs       : net8.0
LOC (.cs)  : 1522
build      : PASS (warnings: 0, errors: 0)
tests      : passed 12 / failed 0 / total 12
```

Migration target: **.NET 10** (all projects → net10.0, packages → 10.x, global.json bumped, build + 12 tests green).

### Model control (read first — this is what keeps the benchmark honest)

This benchmark compares **tooling + process**, not underlying models. Copilot and Claude
Code are both harnesses; the model is a separate variable we hold constant.

- **Pin Claude Sonnet 5 as the main model in every run you can.**
- Runs B and C both run flat Sonnet 5 (`/model sonnet`) — so the ONLY difference between
  them is the guardrail system. Model tiering (Opus-for-planning, layer 7) is deliberately
  LEFT OFF Run C's headline config so there's no "smarter model" confound.
- Run A: select Claude Sonnet in Copilot if its modernize agent exposes a model picker.
  If it doesn't, use whatever it defaults to and **record that model exactly** below.
- Record the exact model + tier each run actually used. Non-negotiable.

---

## RUN A — GitHub Copilot modernize-dotnet   (branch: bench/copilot)

```
Model used (record EXACTLY — e.g. "Claude Sonnet via Copilot picker" or "GPT-5 default, picker not honored"):
Wall time (start → green build+tests):
Setup time (tooling/extension):
Manual interventions (count + one line each):
Build failures during the run (count):
Hallucinated APIs / wrong overloads (count + example):
Oscillation / thrash episodes (count + where):
End state — bench-baseline.sh output:
Defects found in 30-min diff review (count + severity):
Did it update global.json? (Y/N):
Subjective code-quality notes:
What it did BEST:
Where it clearly lost:
Token/compute cost if visible:
```

## RUN B — Claude Code, plain   (branch: bench/claude-plain)

```
Model used (should be: Sonnet 5, via /model sonnet):
Wall time (start → green build+tests):
Setup time (none expected):
Manual interventions (count + one line each):
Build failures during the run (count):
Hallucinated APIs / wrong overloads (count + example):
Oscillation / thrash episodes (count + where):
End state — bench-baseline.sh output:
Defects found in 30-min diff review (count + severity):
Did it update global.json? (Y/N):
Subjective code-quality notes:
What it did BEST:
Where it clearly lost:
Token/compute cost if visible:
📸 Screenshots captured (for flagship post too):
```

## RUN C — Claude Code + 12-layer guardrails   (branch: bench/claude-guarded)

```
Model used      : Claude Sonnet 5, high thinking (/model sonnet) — tiering OFF, same as Run B
Wall time       : 6m 31s
Setup time      : SIGNIFICANT — MCP server wouldn't start under the repo's global.json pin;
                  required switching .mcp.json from `dotnet run --project` to the built DLL
                  (dotnet run resolved SDK 8.0.423 from cwd and couldn't build the net10 server).
                  ~2m of debugging before the guarded run could start.
Interventions   : fully autonomous
Build failures during run : 1
Hallucinated APIs / wrong overloads : none — verified FastEndpoints signatures by reflection
Oscillation / thrash episodes       : none observed
Hooks fired?    : Yes, one, investigation-gate hook
End state       : TFMs net10.0 ×5 | build 0 warnings / 0 errors | tests 12/12 | LOC 1522
global.json updated : YES → 10.0.102
Defects in 30-min diff review : [You fill it]
```

---

## Ground rules (credibility)
- Timebox each run ~2.5h. If it doesn't reach green, THAT is the result — record where it stopped.
- No re-runs for prettier numbers. First attempt counts.
- Concede Copilot's real wins (free, VS-integrated, MS-blessed assessment artifacts).
- Keep all three branches pushed — public branches ARE the reproducibility claim.
