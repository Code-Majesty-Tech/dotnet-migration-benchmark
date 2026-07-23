// Claude Code PostToolUse hook (matcher: Read) — session read-log recorder.
//
// Appends every file the agent Reads to a per-session log, which the
// investigation gate (PreToolUse on Edit|Write) checks before allowing
// modifications. Together they enforce: no edit without evidence.
//
// Zero dependencies. Never blocks anything — exit 0 always.

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

const logFile =
  process.env.READ_LOG_FILE || path.join(os.tmpdir(), `.claude-read-log-${process.ppid}.json`);

try {
  let log = { reads: [] };
  try {
    log = JSON.parse(fs.readFileSync(logFile, 'utf8'));
  } catch {
    /* first read of the session */
  }
  if (!log.reads.includes(filePath)) log.reads.push(filePath);
  fs.writeFileSync(logFile, JSON.stringify(log));
} catch {
  /* recording is best-effort — never break the session */
}
process.exit(0);
