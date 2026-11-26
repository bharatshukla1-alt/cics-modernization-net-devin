#!/bin/sh
# Creates the least-privilege API login (SEC-002) after the schema is loaded.
set -e
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
  -v app_password="$CARDDEMO_APP_PASSWORD" -f /docker-entrypoint-initdb.d/sql/least-privilege-app-role.sql
