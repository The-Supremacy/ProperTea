#!/bin/sh
set -e

echo "🚀 Starting Apps DB Initialization..."

init_app_db() {
    db=$1
    user=$2
    pass=$3

    echo "⚙️  Processing App DB: '$db' (User: $user)..."

    # 1. Create User
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

    # 2. Create DB
    if psql -U "$POSTGRES_USER" -lqt | cut -d \| -f 1 | grep -qw "$db"; then
        echo "   -> Database '$db' exists."
    else
        echo "   -> Creating database '$db'..."
        createdb -U "$POSTGRES_USER" -O "$user" "$db"
    fi

    # 3. Grant Permissions
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db" <<-EOSQL
        GRANT ALL ON SCHEMA public TO $user;
EOSQL
}

# --- Execute for each app ---
init_app_db "propertea_organization_db" "propertea" "$ORG_SERVICE_DB_PASS"

echo "✅ Apps DB Initialization Completed."
