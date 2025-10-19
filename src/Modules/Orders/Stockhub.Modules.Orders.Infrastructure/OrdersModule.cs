using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stockhub.Common.Infrastructure;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Common.Presentation.Endpoints;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Application.OrderValidators;
using Stockhub.Modules.Orders.Application.Services;
using Stockhub.Modules.Orders.Infrastructure.Database;

namespace Stockhub.Modules.Orders.Infrastructure;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddApplicationServices();
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);

        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string dbConnectionString = configuration.GetConnectionStringOrThrow(ServiceNames.PostgresDb);

        services.AddDbContext<OrdersDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(
                    connectionString: dbConnectionString,
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
