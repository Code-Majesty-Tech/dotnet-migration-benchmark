// Claude Code PostToolUse hook (matcher: Edit|Write) — .NET build gate.
//
// Runs `dotnet build` after multi-file edit bursts so compilation failures
// surface while the agent still holds the context that caused them —
// instead of accumulating across ten edits and exploding at the end.
//
// Design (full write-up: https://codemajesty.tech/glossary/posttooluse-build-gate):
//   - One edited file since the last green build -> skip (let the unit of work finish)
//   - A second DISTINCT file, or any .csproj/.sln/migration edit -> run the build
//   - On failure: feed a TRIMMED report back to the agent (first errors, not
//     400 lines of restore output) and keep the touched-file state
//   - On success: reset the touched-file state
//   - Any internal error -> exit 0 (a broken gate must never break the loop)

'use strict';

const fs = require('fs');
const path = require('path');
const os = require('os');
const { execSync } = require('child_process');

const BUILD_TIMEOUT_MS = parseInt(process.env.BUILD_GATE_TIMEOUT_MS ?? '120000', 10);
const MAX_REPORT_LINES = 25;

let data;
try {
  data = JSON.parse(fs.readFileSync(0, 'utf8'));
} catch {
  process.exit(0);
}

const filePath = (data.tool_input || {}).file_path || '';
if (!filePath) process.exit(0);

// Only gate files that participate in compilation.
const relevant = /\.(cs|csproj|sln|slnx|razor|props|targets)$/i.test(filePath);
if (!relevant) process.exit(0);

const stateFile =
  process.env.BUILD_GATE_STATE_FILE ||
  path.join(os.tmpdir(), `.claude-build-gate-${process.ppid}.json`);

function readState() {
  try {
    return JSON.parse(fs.readFileSync(stateFile, 'utf8'));
  } catch {
    return { touched: [] };
  }
}

function writeState(state) {
  try {
    fs.writeFileSync(stateFile, JSON.stringify(state));
  } catch {
    /* best-effort */
  }
}

// Walk up from the edited file to find the nearest solution or project to build.
function findBuildTarget(fromFile) {
  let dir = path.dirname(path.resolve(fromFile));
  let firstProject = null;
  while (dir !== path.dirname(dir)) {
    const entries = fs.readdirSync(dir);
    const sln = entries.find((e) => e.endsWith('.sln') || e.endsWith('.slnx'));
    if (sln) return path.join(dir, sln);
    if (!firstProject) {
      const proj = entries.find((e) => e.endsWith('.csproj'));
      if (proj) firstProject = path.join(dir, proj);
    }
    dir = path.dirname(dir);
  }
  return firstProject;
}

try {
  const state = readState();
  if (!state.touched.includes(filePath)) state.touched.push(filePath);

  const structural = /\.(csproj|sln|slnx|props|targets)$/i.test(filePath) ||
    /migrations/i.test(filePath);

  // First touched file and nothing structural: let the agent finish the unit of work.
  if (state.touched.length < 2 && !structural) {
    writeState(state);
    process.exit(0);
  }

  const target = findBuildTarget(filePath);
  if (!target) {
    writeState(state);
    process.exit(0);
  }

  let output = '';
  let failed = false;
  try {
    output = execSync(`dotnet build "${target}" --nologo -v q`, {
      timeout: BUILD_TIMEOUT_MS,
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe'],
    });
  } catch (e) {
    failed = true;
    output = `${e.stdout || ''}\n${e.stderr || ''}`;
  }

  if (!failed) {
    writeState({ touched: [] }); // green — reset
    process.exit(0);
  }

  // Trim the report: error lines first, first occurrence per file.
  const seen = new Set();
  const errors = output
    .split('\n')
    .filter((l) => /error [A-Z]+\d+/i.test(l))
    .filter((l) => {
      const file = (l.match(/^(.*?)\(/) || [])[1] || l;
      if (seen.has(file)) return false;
      seen.add(file);
      return true;
    })
    .slice(0, MAX_REPORT_LINES);

  writeState(state); // keep touched files — still not green

  console.log(
    JSON.stringify({
      hookSpecificOutput: {
        hookEventName: 'PostToolUse',
        additionalContext:
          `BUILD GATE: the solution does not compile after your recent edits.\n` +
          `Touched since last green build: ${state.touched.map((f) => path.basename(f)).join(', ')}\n` +
          `First error per file:\n${errors.join('\n') || output.slice(-1500)}\n` +
          `Fix the build before moving to the next step.`,
      },
    })
  );
  process.exit(0);
} catch {
  process.exit(0); // fail open, always
}
