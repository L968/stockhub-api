using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stockhub.Aspire.ServiceDefaults;
using Stockhub.Common.Infrastructure;
using Stockhub.Consumers.Configuration;
using Stockhub.Consumers.Consumers;
using Stockhub.Consumers.Database;
using Stockhub.Consumers.Handlers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.Configure<KafkaConsumerSettings>(builder.Configuration.GetSection("Kafka"));

builder.Services.AddHostedService<TradeExecutedConsumer>();
builder.Services.AddScoped<TradeExecutedEventHandler>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("Stockhub.Consumers"));


builder.Services.AddDbContext<OrdersDbContext>((serviceProvider, options) =>
    options.UseNpgsql(ServiceNames.PostgresDb)
);

IHost host = builder.Build();
await host.RunAsync();
