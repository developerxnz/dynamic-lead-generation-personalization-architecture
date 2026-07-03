#!/usr/bin/env bash

set -euo pipefail

OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://ollama:11434}"
TIMEOUT_SECONDS="${OLLAMA_WAIT_TIMEOUT_SECONDS:-180}"
SLEEP_SECONDS=2
ELAPSED=0

echo "Waiting for Ollama at $OLLAMA_BASE_URL"

until curl -fsS "$OLLAMA_BASE_URL/api/tags" >/dev/null 2>&1; do
  if (( ELAPSED >= TIMEOUT_SECONDS )); then
    echo "ERROR: Ollama did not become ready within ${TIMEOUT_SECONDS}s" >&2
    exit 1
  fi

  sleep "$SLEEP_SECONDS"
  ELAPSED=$((ELAPSED + SLEEP_SECONDS))
done

echo "Ollama is ready."
