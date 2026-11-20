-- Runtime login for the MG-03 user administration service: read/write rows of usrsec only.
-- Run as the database owner with
--   psql -v useradmin_password=... -f least-privilege-useradmin-role.sql
SELECT 'CREATE ROLE carddemo_useradmin LOGIN'
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'carddemo_useradmin') \gexec
ALTER ROLE carddemo_useradmin WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT PASSWORD :'useradmin_password';
SELECT format('GRANT CONNECT ON DATABASE %I TO carddemo_useradmin', current_database()) \gexec
GRANT USAGE ON SCHEMA public TO carddemo_useradmin;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM carddemo_useradmin;
GRANT SELECT, INSERT, UPDATE, DELETE ON usrsec TO carddemo_useradmin;
