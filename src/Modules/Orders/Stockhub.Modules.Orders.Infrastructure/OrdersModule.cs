using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stockhub.Common.Application.DomainEvent;
using Stockhub.Common.Infrastructure;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Common.Infrastructure.Outbox;
using Stockhub.Common.Presentation.Endpoints;
using Stockhub.Modules.Orders.Application.Abstractions;
using Stockhub.Modules.Orders.Infrastructure.Database;
using Stockhub.Modules.Orders.Infrastructure.Outbox;

namespace Stockhub.Modules.Orders.Infrastructure;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddDomainEventHandlers();
        services.AddOutboxJob(configuration);
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
                .AddInterceptors(serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>())
        );

        services.AddScoped<IOrdersDbContext>(sp => sp.GetRequiredService<OrdersDbContext>());
    }

    private static void AddDomainEventHandlers(this IServiceCollection services)
    {
        Type[] domainEventHandlers = Application.AssemblyReference.Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IDomainEventHandler)))
            .ToArray();

        foreach (Type domainEventHandler in domainEventHandlers)
        {
            services.TryAddScoped(domainEventHandler);

            Type domainEvent = domainEventHandler
                .GetInterfaces()
                .Single(i => i.IsGenericType)
                .GetGenericArguments()
                .Single();

            Type closedIdempotentHandler = typeof(IdempotentDomainEventHandler<>).MakeGenericType(domainEvent);

            services.Decorate(domainEventHandler, closedIdempotentHandler);
        }
    }

    private static void AddOutboxJob(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(configuration.GetSection("Orders:Outbox"));
        services.ConfigureOptions<ConfigureProcessOutboxJob>();
    }
}
