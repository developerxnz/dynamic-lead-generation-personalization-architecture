#!/usr/bin/env bash

set -euo pipefail

COSMOS_HEALTHCHECK_URL="${COSMOS_HEALTHCHECK_URL:-http://cosmosdb:8080/ready}"
TIMEOUT_SECONDS="${COSMOS_WAIT_TIMEOUT_SECONDS:-180}"
SLEEP_SECONDS=2
ELAPSED=0

echo "Waiting for Cosmos DB Emulator at $COSMOS_HEALTHCHECK_URL"

until curl -fsS "$COSMOS_HEALTHCHECK_URL" >/dev/null 2>&1; do
  if (( ELAPSED >= TIMEOUT_SECONDS )); then
    echo "ERROR: Cosmos DB Emulator did not become ready within ${TIMEOUT_SECONDS}s" >&2
    exit 1
  fi

  sleep "$SLEEP_SECONDS"
  ELAPSED=$((ELAPSED + SLEEP_SECONDS))
done

echo "Cosmos DB Emulator is ready."
