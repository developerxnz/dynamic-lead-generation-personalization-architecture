#!/usr/bin/env bash
# Pull the default model into the Ollama service.
# Run this once after the devcontainer starts.
#
# Usage:
#   ./scripts/pull-model.sh              # pulls the default model
#   ./scripts/pull-model.sh phi4:14b     # pulls a specific model

set -euo pipefail

MODEL="${1:-${OLLAMA_MODEL:-qwen2.5:1.5b}}"
OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://ollama:11434}"

echo "Pulling model: $MODEL"
echo "Ollama endpoint: $OLLAMA_BASE_URL"
echo ""

curl -fsS -X POST "$OLLAMA_BASE_URL/api/pull" \
  -H "Content-Type: application/json" \
  -d "{\"name\": \"$MODEL\"}" \
  --no-buffer | sed -u -n 's/.*"status":"\([^"]*\)".*/\1/p'

echo ""
echo "Done. Available models:"
curl -fsS "$OLLAMA_BASE_URL/api/tags" | tr ',' '\n' | sed -n 's/.*"name":"\([^"]*\)".*/  \1/p'
