#!/bin/bash
set -e

DECLARE_INFRA_SERVICES=(
    "infisical:$INFISICAL_DB_PASS"
    "authentik:$AUTHENTIK_DB_PASS"
    "unleash:$UNLEASH_DB_PASS"
    "openfga:$OPENFGA_DB_PASS"
)

echo "🚀 Starting Infra DB Initialization (Declarative Mode)..."

for entry in "${DECLARE_INFRA_SERVICES[@]}"; do
    IFS=':' read -r service_name password <<< "$entry"

    echo "⚙️  Processing Service: '$service_name'..."

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        DO \$\$
        BEGIN
            IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$service_name') THEN
                CREATE USER $service_name WITH PASSWORD '$password';
                RAISE NOTICE 'User % created', '$service_name';
            ELSE
                ALTER USER $service_name WITH PASSWORD '$password';
                RAISE NOTICE 'User % exists (password updated)', '$service_name';
            END IF;
        END
        \$\$;
EOSQL

    if psql -U "$POSTGRES_USER" -lqt | cut -d \| -f 1 | grep -qw "$service_name"; then
        echo "   -> Database '$service_name' already exists. Skipping."
    else
        echo "   -> Creating database '$service_name'..."
        createdb -U "$POSTGRES_USER" -O "$service_name" "$service_name"
    fi

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$service_name" <<-EOSQL
        GRANT ALL ON SCHEMA public TO $service_name;
EOSQL
done

echo "🏁 Infra DB Initialization Completed."
