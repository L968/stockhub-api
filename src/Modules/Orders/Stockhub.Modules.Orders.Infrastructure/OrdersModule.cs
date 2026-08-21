using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Stockhub.Common.Infrastructure;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Common.Messaging;
using Stockhub.Common.Presentation.Endpoints;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Application.OrderValidators;
using Stockhub.Modules.Orders.Application.Services;
using Stockhub.Modules.Orders.Infrastructure.Database;
using Stockhub.Modules.Orders.Infrastructure.Messaging;

namespace Stockhub.Modules.Orders.Infrastructure;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddMessaging(configuration);
        services.AddApplicationServices();
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);

        return services;
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        string rabbitMqConnectionString = configuration.GetConnectionStringOrThrow(ServiceNames.RabbitMq);

        services.Configure<RabbitMqStreamOptions>(
            configuration.GetSection(RabbitMqStreamOptions.SectionName));
        services.AddSingleton<IOrderStreamPublisher>(serviceProvider =>
            new OrderStreamPublisher(
                rabbitMqConnectionString,
                serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqStreamOptions>>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()));
        services.AddSingleton<IOutboxRepository, OutboxRepository>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string dbConnectionString = configuration.GetConnectionStringOrThrow(ServiceNames.PostgresDb);

        services.AddSingleton(_ => NpgsqlDataSource.Create(dbConnectionString));

        services.AddDbContext<OrdersDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(
                    dataSource: serviceProvider.GetRequiredService<NpgsqlDataSource>(),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, ServiceNames.DatabaseName)
                )
        );

        services.AddScoped<IOrdersDbContext>(sp => sp.GetRequiredService<OrdersDbContext>());
    }

    private static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISideOrderValidator, BuyOrderValidator>();
        services.AddScoped<ISideOrderValidator, SellOrderValidator>();

        services.AddScoped<IOrderValidationService, OrderValidationService>();
    }
}
