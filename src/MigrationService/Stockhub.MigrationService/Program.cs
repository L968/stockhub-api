using Stockhub.Aspire.ServiceDefaults;
using Stockhub.Common.Infrastructure;
using Stockhub.MigrationService;
using Stockhub.Modules.Orders.Infrastructure.Database;
using Stockhub.Modules.Stocks.Infrastructure.Database;
using Stockhub.Modules.Users.Infrastructure.Database;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddNpgsqlDbContext<OrdersDbContext>(ServiceNames.PostgresDb);
builder.AddNpgsqlDbContext<StocksDbContext>(ServiceNames.PostgresDb);
builder.AddNpgsqlDbContext<UsersDbContext>(ServiceNames.PostgresDb);

IHost host = builder.Build();
await host.RunAsync();
