#!/bin/sh
# Creates the least-privilege runtime logins after the schemas are loaded.
set -e
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
  -v app_password="$CARDDEMO_APP_PASSWORD" -f /docker-entrypoint-initdb.d/sql/least-privilege-app-role.sql
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
  -v signon_password="$CARDDEMO_SIGNON_PASSWORD" -f /docker-entrypoint-initdb.d/sql/least-privilege-signon-role.sql
