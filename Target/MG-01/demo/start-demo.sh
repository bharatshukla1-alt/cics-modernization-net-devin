#!/usr/bin/env bash
# One-command MG-01 demo. Needs Docker with Compose v2. Ports: UI 5173, API 5000, DB 5433 (override UI_PORT/API_PORT/DB_PORT).
set -euo pipefail
cd "$(dirname "$0")"
if [ ! -f .env ]; then
  printf 'CARDDEMO_DB_PASSWORD=%s\nCARDDEMO_APP_PASSWORD=%s\n' "$(openssl rand -hex 12)" "$(openssl rand -hex 12)" > .env
  chmod 600 .env
  echo "Created demo/.env with random local passwords."
fi
docker compose up -d --build
echo "Waiting for the API..."
for _ in $(seq 1 60); do
  if curl -fs "http://localhost:${API_PORT:-5000}/health" >/dev/null; then break; fi
  sleep 2
done
curl -fs "http://localhost:${API_PORT:-5000}/health" && echo
echo "MG-01 demo is up: http://localhost:${UI_PORT:-5173}/transaction-types"
echo "Stop with: docker compose -f $(pwd)/docker-compose.yml down   (add -v to reset the database)"
