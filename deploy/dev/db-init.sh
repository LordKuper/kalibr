#!/bin/sh
# Dev copy of the db-init script of deploy/k8s/configmap.yaml: runs once, on an empty data volume, and creates the role the
# app connects with. The heredoc is quoted so that the shell leaves :'app_password' to psql.
psql -v ON_ERROR_STOP=1 -v app_password="$KALIBR_DB_PASSWORD" --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<'SQL'
CREATE ROLE kalibr LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD :'app_password';
ALTER DATABASE kalibr OWNER TO kalibr;
SQL
