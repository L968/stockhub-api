using Stockhub.Common.Infrastructure;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("postgresPassword", "root", secret: true);
IResourceBuilder<ParameterResource> redisPassword = builder.AddParameter("redisPassword", "admin", secret: true);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres(ServiceNames.Postgres, password: postgresPassword, port: 5432)
    .WithImageTag("18.0")
    .WithPgWeb()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<PostgresDatabaseResource> productsDb = postgres.AddDatabase(ServiceNames.PostgresDb, ServiceNames.DatabaseName);

IResourceBuilder<RedisResource> redis = builder.AddRedis(ServiceNames.Redis, password: redisPassword)
    .WithImageTag("7.4.2")
    .WithRedisInsight(insight => insight.WithHostPort(5540))
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.Stockhub_Api>(ServiceNames.Api)
    .WithReference(productsDb)
        .WaitFor(productsDb)
    .WithReference(redis)
        .WaitFor(redis);

builder.AddProject<Projects.Stockhub_MigrationService>(ServiceNames.MigrationService)
    .WithReference(productsDb)
        .WaitFor(productsDb);

await builder.Build().RunAsync();
