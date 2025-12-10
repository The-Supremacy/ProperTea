#!/bin/bash
set -e

create_user_and_db() {
    local user=$1
    local pass=$2
    local db=$3

    echo "⚙️  Configuring '$db'..."

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        DO \$\$
        BEGIN
            IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$user') THEN
                CREATE USER $user WITH PASSWORD '$pass';
                RAISE NOTICE 'User % created', '$user';
            ELSE
                ALTER USER $user WITH PASSWORD '$pass'; -- Ensure password is up to date
                RAISE NOTICE 'User % already exists (password updated)', '$user';
            END IF;
        END
        \$\$;
EOSQL

    if psql -U "$POSTGRES_USER" -lqt | cut -d \| -f 1 | grep -qw "$db"; then
        echo "   Database '$db' already exists. Skipping."
    else
        echo "   Creating database '$db'..."
        createdb -U "$POSTGRES_USER" -O "$user" "$db"
    fi

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db" <<-EOSQL
        GRANT ALL ON SCHEMA public TO $user;
EOSQL
    echo "   ✅ Done."
}

echo "🚀 Starting Infra DB Initialization (Safe Mode)..."

create_user_and_db "infisical" "$INFISICAL_DB_PASS" "infisical"
create_user_and_db "authentik" "$AUTHENTIK_DB_PASS" "authentik"
create_user_and_db "unleash"   "$UNLEASH_DB_PASS"   "unleash"
create_user_and_db "openfga"   "$OPENFGA_DB_PASS"   "openfga"

echo "🏁 Infra DB Initialization Completed."
