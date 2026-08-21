using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Stockhub.Aspire.ServiceDefaults;
using Stockhub.Common.Infrastructure;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Common.Messaging;
using Stockhub.Consumers.MatchingEngine.Application.Services;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Database.Interfaces;
using Stockhub.Consumers.MatchingEngine.Infrastructure.Workers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

string dbConnectionString = builder.Configuration.GetConnectionStringOrThrow(ServiceNames.PostgresDb);
string rabbitMqConnectionString = builder.Configuration.GetConnectionStringOrThrow(ServiceNames.RabbitMq);

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(dbConnectionString));

builder.Services.Configure<RabbitMqStreamOptions>(
    builder.Configuration.GetSection(RabbitMqStreamOptions.SectionName));

builder.Services.AddSingleton<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<ITradeSettlementRepository, TradeSettlementRepository>();
builder.Services.AddSingleton<ITradeExecutor, TradeExecutor>();

builder.Services.AddSingleton<IOrderBookRepository, OrderBookRepository>();
builder.Services.AddSingleton<IMatchingEngineService, MatchingEngineService>();

builder.Services.AddSingleton(serviceProvider => new MatchingWorkerHostedService(
    rabbitMqConnectionString,
    serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqStreamOptions>>(),
    serviceProvider.GetRequiredService<IOrderRepository>(),
    serviceProvider.GetRequiredService<IOrderBookRepository>(),
    serviceProvider.GetRequiredService<IMatchingEngineService>(),
    serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>(),
    serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MatchingWorkerHostedService>>()));
builder.Services.AddHostedService(serviceProvider =>
    serviceProvider.GetRequiredService<MatchingWorkerHostedService>());

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("Stockhub.Consumers.MatchingEngine"));

IHost host = builder.Build();
await host.RunAsync();
