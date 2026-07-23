// Claude Code PreToolUse hook (matcher: Edit|Write) — investigation gate.
//
// Blocks edits to files the agent has not Read in this session, enforcing
// "no edit without evidence" mechanically instead of hoping the CLAUDE.md
// request is honored under context pressure.
//
// This is the file-level starter version. The production version we run
// gates on SYMBOL references (blocking edits that call methods the agent
// never read), which requires semantic analysis — see:
// https://codemajesty.tech/glossary/investigation-gate-hook
//
// Rules:
//   - Editing an existing file that was never Read  -> block, with remediation
//   - Writing a brand-new file                      -> allow (nothing to read)
//   - Any internal error                            -> allow (fail open)
//
// Blocks with exit 2 + a JSON decision; allows with exit 0.

'use strict';

const fs = require('fs');
const path = require('path');
const os = require('os');

let data;
try {
  data = JSON.parse(fs.readFileSync(0, 'utf8'));
} catch {
  process.exit(0);
}

const filePath = (data.tool_input || {}).file_path || '';
if (!filePath) process.exit(0);

// Brand-new file: there is nothing to investigate — allow.
if (!fs.existsSync(filePath)) process.exit(0);

const logFile =
  process.env.READ_LOG_FILE || path.join(os.tmpdir(), `.claude-read-log-${process.ppid}.json`);

try {
  let reads = [];
  try {
    reads = JSON.parse(fs.readFileSync(logFile, 'utf8')).reads || [];
  } catch {
    /* no log yet — treat as nothing read */
  }

  if (reads.includes(filePath)) process.exit(0);

  const fileName = path.basename(filePath);
  console.log(
    JSON.stringify({
      hookSpecificOutput: {
        hookEventName: 'PreToolUse',
        decision: 'block',
        reason:
          `INVESTIGATION GATE: you are editing ${fileName} without having Read it ` +
          `in this session. Read the file first, verify the symbols you plan to ` +
          `touch actually exist with the signatures you expect, then retry the edit.`,
      },
    })
  );
  process.exit(2);
} catch {
  // Never block due to a hook error — fail open.
  process.exit(0);
}
