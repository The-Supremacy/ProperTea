#!/bin/bash
set -e

# Function to safely create user and db
create_user_and_db() {
    local user=$1
    local pass=$2
    local db=$3

    echo "  Creating App User '$user' and Database '$db'..."

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        CREATE USER $user WITH PASSWORD '$pass';
        CREATE DATABASE $db;
        GRANT ALL PRIVILEGES ON DATABASE $db TO $user;
EOSQL

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db" <<-EOSQL
        GRANT ALL ON SCHEMA public TO $user;
EOSQL
}

echo "🚀 Starting Apps DB Initialization..."

# Organization Service
create_user_and_db "propertea" "$ORG_SERVICE_DB_PASS" "propertea_organization_db"

echo "✅ Apps DB Initialization Completed."
