#!/bin/bash

set -e

echo "Initializing databases..."

psql -v ON_ERROR_STOP=1 --username "postgres" <<-EOSQL
    SELECT 'CREATE DATABASE user_db' WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'user_db')\\gexec
EOSQL

echo "Databases initialized successfully!"

