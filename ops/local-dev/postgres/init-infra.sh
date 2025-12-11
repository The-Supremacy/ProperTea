#!/bin/sh
set -e

echo "🚀 Starting Infra DB Initialization..."

# Function to handle logic for each service
init_service_db() {
    service_name=$1
    password=$2

    echo "⚙️  Processing Service: '$service_name'..."

    # 1. Create User
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        DO \$\$
        BEGIN
            IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$service_name') THEN
                CREATE USER $service_name WITH PASSWORD '$password';
            ELSE
                ALTER USER $service_name WITH PASSWORD '$password';
            END IF;
        END
        \$\$;
EOSQL

    # 2. Create DB
    if psql -U "$POSTGRES_USER" -lqt | cut -d \| -f 1 | grep -qw "$service_name"; then
        echo "   -> Database '$service_name' already exists. Skipping."
    else
        echo "   -> Creating database '$service_name'..."
        createdb -U "$POSTGRES_USER" -O "$service_name" "$service_name"
    fi

    # 3. Grant Permissions
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$service_name" <<-EOSQL
        GRANT ALL ON SCHEMA public TO $service_name;
EOSQL
}

# --- Execute for each service ---
init_service_db "infisical" "$INFISICAL_DB_PASS"
init_service_db "authentik" "$AUTHENTIK_DB_PASS"
init_service_db "unleash" "$UNLEASH_DB_PASS"
init_service_db "openfga" "$OPENFGA_DB_PASS"

echo "🏁 Infra DB Initialization Completed."
