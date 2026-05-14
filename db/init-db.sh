#!/bin/bash
set -e

echo "Waiting for SQL Server"

for i in {1..60}; do
  /opt/mssql-tools18/bin/sqlcmd \
    -S db,1433 \
    -U sa \
    -P "${MSSQL_SA_PASSWORD}" \
    -C \
    -d master \
    -Q "SELECT 1" && break

  echo "SQL Server not ready, waiting..."
  sleep 2
done

echo "SQL Server ready. Starting DB Init"

echo "Creating DB"
/opt/mssql-tools18/bin/sqlcmd \
  -S db,1433 \
  -U sa \
  -P "${MSSQL_SA_PASSWORD}" \
  -C \
  -d master \
  -v DB_NAME="${DB_NAME}" \
  -i /db/scripts/1_create_database.sql

echo "Creating schema and tables"
/opt/mssql-tools18/bin/sqlcmd \
  -S db,1433 \
  -U sa \
  -P "${MSSQL_SA_PASSWORD}" \
  -C \
  -d "${DB_NAME}" \
  -v DB_NAME="${DB_NAME}" \
  -i /db/scripts/2_create_schema.sql

echo "Seeding data"
/opt/mssql-tools18/bin/sqlcmd \
  -S db,1433 \
  -U sa \
  -P "${MSSQL_SA_PASSWORD}" \
  -C \
  -d "${DB_NAME}" \
  -v DB_NAME="${DB_NAME}" \
  -i /db/scripts/3_seed_data.sql

echo "DB ready"