using Stockhub.Aspire.ServiceDefaults;
using Stockhub.Common.Infrastructure;
using Stockhub.MigrationService;
using Stockhub.Modules.Orders.Infrastructure.Database;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddNpgsqlDbContext<OrdersDbContext>(ServiceNames.PostgresDb);

IHost host = builder.Build();
await host.RunAsync();
