#!/bin/sh
set -eu
# The runtime login can manage its own tables for local migrations, but is not a PostgreSQL superuser.
psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set=app_password="$DINDER_DATABASE_PASSWORD" --set=ON_ERROR_STOP=1 <<'SQL'
CREATE ROLE dinder LOGIN PASSWORD :'app_password' NOSUPERUSER NOCREATEDB NOCREATEROLE;
GRANT CONNECT ON DATABASE dinder TO dinder;
GRANT USAGE, CREATE ON SCHEMA public TO dinder;
SQL
