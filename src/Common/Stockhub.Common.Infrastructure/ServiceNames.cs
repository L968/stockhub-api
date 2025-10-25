namespace Stockhub.Common.Infrastructure;

public static class ServiceNames
{
    public const string Postgres = "stockhub-postgres";
    public const string PostgresDb = "stockhub-postgresdb";
    public const string DatabaseName = "stockhub";
    public const string Redis = "stockhub-redis";

    public const string Api = "stockhub-api";
    public const string ConsumerMatchingEngine = "stockhub-consumer-matching-engine";
    public const string MigrationService = "stockhub-migrationservice";
}
