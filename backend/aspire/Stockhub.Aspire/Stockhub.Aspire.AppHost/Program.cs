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

IResourceBuilder<ProjectResource> api = builder.AddProject<Projects.Stockhub_Api>(ServiceNames.Api)
    .WithEndpoint("http", endpoint => endpoint.Port = 5196)
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

builder.AddViteApp("frontend", "../../../../frontend")
    .WithReference(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WaitFor(api);

await builder.Build().RunAsync();
