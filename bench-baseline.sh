#!/usr/bin/env bash
# bench-baseline.sh — capture the measurable state of this repo at the current
# commit/branch. Run it on the green baseline (record the numbers), then again
# after each migration run to capture the "after" state for the three-way
# comparison. Output is a compact block you paste into RESULTS.md.
#
# Usage:  ./bench-baseline.sh            # human-readable to stdout
#         ./bench-baseline.sh --json     # machine-readable
set -uo pipefail
cd "$(dirname "$0")"

JSON=false; [ "${1:-}" = "--json" ] && JSON=true

COMMIT=$(git rev-parse --short HEAD 2>/dev/null || echo "?")
BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "?")
SDK=$(dotnet --version 2>/dev/null || echo "?")
TFMS=$(grep -rh "TargetFramework" --include="*.csproj" . 2>/dev/null | grep -oE "net[0-9]+\.[0-9]+" | sort -u | paste -sd, -)
LOC=$(find src tests -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" 2>/dev/null | xargs wc -l 2>/dev/null | tail -1 | awk '{print $1}')

BUILD_OUT=$(dotnet build --nologo 2>&1)
WARN=$(echo "$BUILD_OUT" | grep -oE "[0-9]+ Warning" | tail -1 | grep -oE "[0-9]+" || echo "?")
ERR=$(echo "$BUILD_OUT" | grep -oE "[0-9]+ Error" | tail -1 | grep -oE "[0-9]+" || echo "?")
BUILD=$([ "$ERR" = "0" ] && echo "PASS" || echo "FAIL")

if [ "$BUILD" = "PASS" ]; then
  TEST_OUT=$(dotnet test --nologo 2>&1)
  TPASS=$(echo "$TEST_OUT" | grep -oE "Passed:[[:space:]]+[0-9]+" | tail -1 | grep -oE "[0-9]+" || echo "?")
  TFAIL=$(echo "$TEST_OUT" | grep -oE "Failed:[[:space:]]+[0-9]+" | tail -1 | grep -oE "[0-9]+" || echo "?")
  TTOTAL=$(echo "$TEST_OUT" | grep -oE "Total:[[:space:]]+[0-9]+" | tail -1 | grep -oE "[0-9]+" || echo "?")
else
  TPASS="-"; TFAIL="-"; TTOTAL="build failed"
fi

if $JSON; then
  printf '{"branch":"%s","commit":"%s","sdk":"%s","tfms":"%s","loc":"%s","build":"%s","warnings":"%s","errors":"%s","tests_passed":"%s","tests_failed":"%s","tests_total":"%s"}\n' \
    "$BRANCH" "$COMMIT" "$SDK" "$TFMS" "$LOC" "$BUILD" "$WARN" "$ERR" "$TPASS" "$TFAIL" "$TTOTAL"
else
  echo "--------------------------------------------"
  echo " branch     : $BRANCH ($COMMIT)"
  echo " dotnet SDK : $SDK"
  echo " TFMs       : $TFMS"
  echo " LOC (.cs)  : $LOC"
  echo " build      : $BUILD  (warnings: $WARN, errors: $ERR)"
  echo " tests      : passed $TPASS / failed $TFAIL / total $TTOTAL"
  echo "--------------------------------------------"
fi
