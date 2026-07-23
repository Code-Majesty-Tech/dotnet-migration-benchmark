// Claude Code PreToolUse hook (matcher: Edit) — oscillation circuit breaker.
//
// Detects A→B→A edit oscillation patterns and blocks the edit with a
// diagnostic message. Tracks edits in a per-session temp file keyed by PPID.
//
// Full write-up of the failure mode and the design:
// https://codemajesty.tech/blog/oscillation-detector-hook-for-claude-code
//
// PreToolUse on Edit — blocks with exit 2, allows with exit 0.
// Never blocks due to internal errors (exit 0 on any exception).

'use strict';

const fs = require('fs');
const path = require('path');
const os = require('os');

const MAX_EDITS_PER_FILE = 20;
const RESET_WINDOW_MS = parseInt(process.env.OSCILLATION_RESET_MS ?? '600000', 10);

// ---- Read Edit tool input from stdin ----

let data;
try {
  data = JSON.parse(fs.readFileSync(0, 'utf8'));
} catch {
  process.exit(0); // No stdin or invalid JSON — allow
}

const toolInput = data.tool_input || {};
const filePath = toolInput.file_path || '';
const oldString = toolInput.old_string || '';
const newString = toolInput.new_string || '';
const replaceAll = toolInput.replace_all === true;

// Skip detection for empty paths, missing old_string, or replace_all (bulk refactors)
if (!filePath || !oldString || replaceAll) {
  process.exit(0);
}

// ---- Simple djb2 string hash ----

function hashString(str) {
  let hash = 5381;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) + hash) + str.charCodeAt(i);
    hash = hash & hash; // Convert to 32-bit integer
  }
  return hash.toString(36);
}

function editHash(oldStr, newStr) {
  const sample = oldStr.slice(0, 200) + '|||' + newStr.slice(0, 200);
  return hashString(sample);
}

// ---- Edit history file management ----

const ppid = process.ppid;
const historyFile = process.env.EDIT_HISTORY_FILE || path.join(os.tmpdir(), `.edit-history-${ppid}.json`);

function readHistory() {
  try {
    return JSON.parse(fs.readFileSync(historyFile, 'utf8'));
  } catch {
    return { edits: [] };
  }
}

function writeHistory(history) {
  try {
    fs.writeFileSync(historyFile, JSON.stringify(history));
  } catch {
    // Temp write failure — non-critical, skip
  }
}

// ---- Oscillation detection ----

function detectOscillation(history, file, currentOldStr, currentNewStr) {
  const now = Date.now();
  const currentReverse = editHash(currentNewStr, currentOldStr);

  // Get recent edits for this file within the reset window
  const recentEdits = history.edits
    .filter(e => e.file === file && (now - e.timestamp) < RESET_WINDOW_MS);

  if (recentEdits.length < 1) return false;

  // Check if current edit reverses any recent edit
  for (let i = recentEdits.length - 1; i >= 0; i--) {
    if (recentEdits[i].transition_hash === currentReverse) {
      return recentEdits.length + 1;
    }
  }
  return false;
}

// ---- Main logic ----

try {
  const history = readHistory();
  const now = Date.now();

  // Detect oscillation BEFORE recording this edit
  const oscillationCount = detectOscillation(history, filePath, oldString, newString);

  if (oscillationCount) {
    const fileName = path.basename(filePath);
    console.log(JSON.stringify({
      hookSpecificOutput: {
        hookEventName: 'PreToolUse',
        decision: 'block',
        reason: `OSCILLATION DETECTED in ${fileName}: you've changed this code back and forth ${oscillationCount} times. Stop and rethink your approach. Consider: (1) what is the root cause of the conflict? (2) is there a different solution that avoids toggling between these two states? (3) should you ask the user for clarification before proceeding?`
      }
    }));
    process.exit(2);
  }

  // Not oscillating — record the edit
  const newEdit = {
    file: filePath,
    transition_hash: editHash(oldString, newString),
    timestamp: now
  };

  history.edits.push(newEdit);

  // Trim to keep last MAX_EDITS_PER_FILE for this file
  const fileEdits = history.edits.filter(e => e.file === filePath);
  if (fileEdits.length > MAX_EDITS_PER_FILE) {
    const oldest = fileEdits[0];
    const idx = history.edits.indexOf(oldest);
    if (idx !== -1) {
      history.edits.splice(idx, 1);
    }
  }

  // Prune entries older than the reset window across all files
  history.edits = history.edits.filter(e => (now - e.timestamp) < RESET_WINDOW_MS);

  writeHistory(history);
  process.exit(0);
} catch {
  // Never block due to hook error
  process.exit(0);
}
