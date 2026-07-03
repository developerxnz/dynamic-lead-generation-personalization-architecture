#!/usr/bin/env bash

set -euo pipefail

MODEL="${1:-${OLLAMA_MODEL:-llama3.1:8b}}"
OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://ollama:11434}"

"$(dirname "$0")/wait-for-ollama.sh"

if curl -fsS "$OLLAMA_BASE_URL/api/tags" | python3 -c '
import json
import sys

model = sys.argv[1]
data = json.load(sys.stdin)
models = {item.get("name") for item in data.get("models", [])}
sys.exit(0 if model in models else 1)
' "$MODEL"
then
  echo "Model already available: $MODEL"
  exit 0
fi

"$(dirname "$0")/pull-model.sh" "$MODEL"
