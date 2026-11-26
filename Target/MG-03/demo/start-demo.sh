#!/usr/bin/env bash
# One-command MG-03 demo (includes MG-01 and MG-02). Needs Docker with Compose v2.
# Ports: UI 5173, SignOnService 5001, TransactionTypeService 5000, UserAdminService 5002, DB 5433 (override UI_PORT/SIGNON_PORT/API_PORT/USERADMIN_PORT/DB_PORT).
set -euo pipefail
cd "$(dirname "$0")"
if [ ! -f .env ]; then
  printf 'CARDDEMO_DB_PASSWORD=%s\nCARDDEMO_APP_PASSWORD=%s\nCARDDEMO_SIGNON_PASSWORD=%s\nCARDDEMO_USERADMIN_PASSWORD=%s\n' \
    "$(openssl rand -hex 12)" "$(openssl rand -hex 12)" "$(openssl rand -hex 12)" "$(openssl rand -hex 12)" > .env
  chmod 600 .env
  echo "Created demo/.env with random local passwords."
fi
if [ ! -f keys/signon.pem ]; then
  mkdir -p keys
  openssl genrsa -out keys/signon.pem 2048 2>/dev/null
  chmod 644 keys/signon.pem   # demo only: readable by the non-root container user
  echo "Created demo/keys/signon.pem (local demo signing key, git-ignored)."
fi
docker compose up -d --build
echo "Waiting for the services..."
for port in "${SIGNON_PORT:-5001}" "${API_PORT:-5000}" "${USERADMIN_PORT:-5002}"; do
  for _ in $(seq 1 60); do
    if curl -fs "http://localhost:${port}/health" >/dev/null; then break; fi
    sleep 2
  done
  curl -fs "http://localhost:${port}/health" && echo
done
echo "CardDemo demo is up: http://localhost:${UI_PORT:-5173}/signin  (ADMIN001 / PASSWORD or USER0001 / PASSWORD)"
echo "Stop with: docker compose -f $(pwd)/docker-compose.yml down   (add -v to reset the database)"
