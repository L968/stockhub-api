using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stockhub.Aspire.ServiceDefaults;
using Stockhub.Common.Infrastructure;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Common.Messaging.Consumers.Configuration;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Kafka;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Kafka.Mappers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

KafkaSettings kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>()!;
builder.Services.AddSingleton(kafkaSettings);

builder.Services.AddSingleton<IMatchingEngine, MatchingEngine>();
builder.Services.AddScoped<OrderPlacedMapper>();
builder.Services.AddHostedService<OrderCdcConsumer>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("Stockhub.Consumers.MatchingEngine"));

string dbConnectionString = builder.Configuration.GetConnectionStringOrThrow(ServiceNames.PostgresDb);

builder.Services.AddDbContext<OrdersDbContext>((serviceProvider, options) =>
    options.UseNpgsql(dbConnectionString)
);

builder.Services.AddDbContext<UsersDbContext>((sp, options) =>
    options.UseNpgsql(dbConnectionString)
);

IHost host = builder.Build();
await host.RunAsync();
