using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stockhub.Aspire.ServiceDefaults;
using Stockhub.Common.Infrastructure;
using Stockhub.Consumers.Configuration;
using Stockhub.Consumers.Consumers;
using Stockhub.Consumers.Database;
using Stockhub.Consumers.Events.OrderPlaced;
using Stockhub.Consumers.Extensions;
using Stockhub.Consumers.Matching;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

KafkaSettings kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>()!;
builder.Services.AddSingleton(kafkaSettings);

builder.Services.AddSingleton<IMatchingEngine, MatchingEngine>();
builder.Services.AddScoped<OrderPlacedMapper>();
builder.Services.AddHostedService<OrderCdcConsumer>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("Stockhub.Consumers"));

string dbConnectionString = builder.Configuration.GetConnectionStringOrThrow(ServiceNames.PostgresDb);

builder.Services.AddDbContext<OrdersDbContext>((serviceProvider, options) =>
    options.UseNpgsql(dbConnectionString)
);

builder.Services.AddDbContext<UsersDbContext>((sp, options) =>
    options.UseNpgsql(dbConnectionString)
);

IHost host = builder.Build();
await host.RunAsync();
