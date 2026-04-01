#!/bin/bash
set -euo pipefail

mkdir -p .cursor/logs

INPUT="$(cat)"

# Save the raw stop event too
printf '%s\n' "$INPUT" >> .cursor/logs/hook-events.jsonl

# Pull transcript_path out of the stop payload
TRANSCRIPT_PATH="$(printf '%s' "$INPUT" | python3 -c 'import sys, json; print(json.load(sys.stdin).get("transcript_path",""))')"

if [ -n "$TRANSCRIPT_PATH" ] && [ -f "$TRANSCRIPT_PATH" ]; then
  {
    echo "===== $(date -Iseconds) ====="
    echo "TRANSCRIPT: $TRANSCRIPT_PATH"
    tail -n 40 "$TRANSCRIPT_PATH"
    echo
  } >> .cursor/logs/agent_log.txt
fi

printf '{"continue":true}\n'