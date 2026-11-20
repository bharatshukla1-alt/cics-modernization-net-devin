-- MG-01 runtime login with least privilege (SEC-002, PCI-DSS 7.2).
-- The schema owner / superuser is used only to provision the schema; the API connects as carddemo_app.
-- Run with psql, supplying the password at run time (never commit it):
--   psql -v app_password="$CARDDEMO_APP_PASSWORD" -d carddemo -f least-privilege-app-role.sql
\set ON_ERROR_STOP on

SELECT format('CREATE ROLE carddemo_app LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT', :'app_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'carddemo_app') \gexec

ALTER ROLE carddemo_app WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT PASSWORD :'app_password';

REVOKE ALL ON DATABASE carddemo FROM PUBLIC;
GRANT CONNECT ON DATABASE carddemo TO carddemo_app;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO carddemo_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE transaction_type TO carddemo_app;
