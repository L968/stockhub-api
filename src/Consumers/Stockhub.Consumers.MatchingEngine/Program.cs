using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Stockhub.Aspire.ServiceDefaults;
using Stockhub.Common.Infrastructure;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Common.Messaging.Consumers.Configuration;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Application.Validators;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Kafka;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Kafka.Mappers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

KafkaSettings kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>()!;
builder.Services.AddSingleton(kafkaSettings);

string dbConnectionString = builder.Configuration.GetConnectionStringOrThrow(ServiceNames.PostgresDb);
builder.Services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(dbConnectionString));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderBookRepository, OrderBookRepository>();

builder.Services.AddScoped<OrderValidator>();

builder.Services.AddSingleton<IMatchingEngineService, MatchingEngineService>();

builder.Services.AddScoped<OrderPlacedMapper>();
builder.Services.AddHostedService<OrderCdcConsumer>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("Stockhub.Consumers.MatchingEngine"));

IHost host = builder.Build();
await host.RunAsync();
