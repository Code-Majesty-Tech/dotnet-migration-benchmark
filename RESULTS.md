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

---

## RUN A — GitHub Copilot modernize-dotnet   (branch: bench/copilot)

```
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
Wall time (start → green build+tests):
Setup time (instrumentation — report honestly, counts separately):
Manual interventions (count + one line each):
Build failures during the run (count):
Hallucinated APIs / wrong overloads (count + example):
Oscillation / thrash episodes (count + where — did the hook fire?):
End state — bench-baseline.sh output:
Defects found in 30-min diff review (count + severity):
Did it update global.json? (Y/N):
Subjective code-quality notes:
What it did BEST:
Where it clearly lost:
Token/compute cost if visible:
📸 Screenshots captured (gate/hook firing = gold):
```

---

## Ground rules (credibility)
- Timebox each run ~2.5h. If it doesn't reach green, THAT is the result — record where it stopped.
- No re-runs for prettier numbers. First attempt counts.
- Concede Copilot's real wins (free, VS-integrated, MS-blessed assessment artifacts).
- Keep all three branches pushed — public branches ARE the reproducibility claim.
