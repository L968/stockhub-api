using Aspire.Hosting.ApplicationModel;
using Stockhub.Common.Infrastructure;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresDatabaseResource> database = builder
    .AddPostgres(ServiceNames.Postgres)
    .AddDatabase(ServiceNames.PostgresDb, ServiceNames.DatabaseName);

IResourceBuilder<RabbitMQServerResource> rabbitMq = builder
    .AddRabbitMQ(ServiceNames.RabbitMq)
    .WithManagementPlugin()
    .WithBindMount("./rabbitmq/enabled_plugins", "/etc/rabbitmq/enabled_plugins", isReadOnly: true)
    .WithEndpoint(port: 5552, targetPort: 5552, name: "stream");

IResourceBuilder<ProjectResource> migrationService = builder
    .AddProject<Projects.Stockhub_MigrationService>(ServiceNames.MigrationService)
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.Stockhub_Api>(ServiceNames.Api)
    .WithReference(database)
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WaitForCompletion(migrationService);

builder.AddProject<Projects.Stockhub_Consumers_MatchingEngine>(ServiceNames.ConsumerMatchingEngine)
    .WithReference(database)
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WaitForCompletion(migrationService)
    .WithReplicas(3);

await builder.Build().RunAsync();
