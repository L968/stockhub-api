#!/bin/bash
set -e

echo "Configuring wal_level to 'logical' and adjusting limits using ALTER SYSTEM..."

# Alters configurations in postgresql.conf using ALTER SYSTEM
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    ALTER SYSTEM SET wal_level TO 'logical';
    ALTER SYSTEM SET max_wal_senders TO '10';
    ALTER SYSTEM SET max_replication_slots TO '10';

    -- Tell the running server to reload its configuration files.
    -- While the 'wal_level' change requires a full restart, the Docker
    -- entrypoint will handle the final restart after this script completes.
    SELECT pg_reload_conf();
EOSQL

echo "WAL configurations written. The PostgreSQL server will apply them during the final container startup."