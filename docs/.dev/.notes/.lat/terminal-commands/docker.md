# in container terminal

export PGPASSWORD=wabbitbot
psql -h host.docker.internal -U wabbitbot -d wabbitbot -c 'SELECT "MigrationId","ProductVersion" FROM "public"."__EFMigrationsHistory" ORDER BY "MigrationId" DESC;'
exit

# from project root

docker run --rm -e PGPASSWORD=wabbitbot postgres:16-alpine psql -h host.docker.internal -U wabbitbot -d wabbitbot -c "SELECT schema_version, applied_at, applied_by, migration_name FROM schema_metadata ORDER BY applied_at;"