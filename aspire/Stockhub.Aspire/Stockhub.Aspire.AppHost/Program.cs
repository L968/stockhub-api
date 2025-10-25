using Stockhub.Common.Infrastructure;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Stockhub_Api>(ServiceNames.Api);

builder.AddProject<Projects.Stockhub_Consumers>(ServiceNames.Consumers);

builder.AddProject<Projects.Stockhub_MigrationService>(ServiceNames.MigrationService);

await builder.Build().RunAsync();
