-- Runtime login for SignOnService: read-only on usrsec. Run as the database owner with
--   psql -v signon_password=... -f least-privilege-signon-role.sql
SELECT 'CREATE ROLE carddemo_signon LOGIN'
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'carddemo_signon') \gexec
ALTER ROLE carddemo_signon WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT PASSWORD :'signon_password';
SELECT format('GRANT CONNECT ON DATABASE %I TO carddemo_signon', current_database()) \gexec
GRANT USAGE ON SCHEMA public TO carddemo_signon;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM carddemo_signon;
GRANT SELECT ON usrsec TO carddemo_signon;
