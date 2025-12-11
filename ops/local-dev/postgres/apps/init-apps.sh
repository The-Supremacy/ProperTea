#!/bin/bash
set -e

DECLARE_APP_SERVICES=(
    "propertea_organization_db:propertea:$ORG_SERVICE_DB_PASS"
)

echo "🚀 Starting Apps DB Initialization..."

for entry in "${DECLARE_APP_SERVICES[@]}"; do
    IFS=':' read -r db user pass <<< "$entry"

    echo "⚙️  Processing App DB: '$db' (User: $user)..."

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        DO \$\$
        BEGIN
            IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$user') THEN
                CREATE USER $user WITH PASSWORD '$pass';
            ELSE
                ALTER USER $user WITH PASSWORD '$pass';
            END IF;
        END
        \$\$;
EOSQL

    if psql -U "$POSTGRES_USER" -lqt | cut -d \| -f 1 | grep -qw "$db"; then
        echo "   -> Database '$db' exists."
    else
        echo "   -> Creating database '$db'..."
        createdb -U "$POSTGRES_USER" -O "$user" "$db"
    fi

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db" <<-EOSQL
        GRANT ALL ON SCHEMA public TO $user;
EOSQL
done

echo "✅ Apps DB Initialization Completed."
