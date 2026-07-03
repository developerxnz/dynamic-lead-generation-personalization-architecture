#!/usr/bin/env bash
# Pull the default model into the Ollama service.
# Run this once after the devcontainer starts.
#
# Usage:
#   ./scripts/pull-model.sh              # pulls the default model
#   ./scripts/pull-model.sh phi4:14b     # pulls a specific model

set -euo pipefail

MODEL="${1:-llama3.1:8b}"
OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://ollama:11434}"

echo "Pulling model: $MODEL"
echo "Ollama endpoint: $OLLAMA_BASE_URL"
echo ""

curl -s -X POST "$OLLAMA_BASE_URL/api/pull" \
  -H "Content-Type: application/json" \
  -d "{\"name\": \"$MODEL\"}" \
  --no-buffer | grep -v '"status":"success"' || true

echo ""
echo "Done. Available models:"
curl -s "$OLLAMA_BASE_URL/api/tags" | python3 -c "
import sys, json
data = json.load(sys.stdin)
for m in data.get('models', []):
    print(f\"  {m['name']}\")
"
