#!/bin/bash
set -euo pipefail

mkdir -p .cursor/logs

INPUT="$(cat)"

# Save the raw prompt event
printf '%s\n' "$INPUT" >> .cursor/logs/hook-events.jsonl

# Tell Cursor to continue
printf '{"continue":true}\n'