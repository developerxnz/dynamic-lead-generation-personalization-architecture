#!/usr/bin/env bash

set -euo pipefail

MODEL="${1:-${OLLAMA_MODEL:-qwen2.5:1.5b}}"
OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://ollama:11434}"

"$(dirname "$0")/wait-for-ollama.sh"

if curl -fsS "$OLLAMA_BASE_URL/api/tags" | grep -F "\"name\":\"$MODEL\"" >/dev/null; then
  echo "Model already available: $MODEL"
  exit 0
fi

"$(dirname "$0")/pull-model.sh" "$MODEL"
