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
using Stockhub.Modules.Users.Application;
using Stockhub.Modules.Users.Application.Abstractions;
using Stockhub.Modules.Users.Infrastructure.Database;
using Stockhub.Modules.Users.Infrastructure.Outbox;

namespace Stockhub.Modules.Users.Infrastructure;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
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

        services.AddDbContext<UsersDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(
                    connectionString: dbConnectionString,
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, ServiceNames.DatabaseName)
                )
                .AddInterceptors(serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>())
        );

        services.AddScoped<IUsersDbContext>(sp => sp.GetRequiredService<UsersDbContext>());
    }

    private static void AddDomainEventHandlers(this IServiceCollection services)
    {
        Type[] domainEventHandlers = AssemblyReference.Assembly
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
        services.Configure<OutboxOptions>(configuration.GetSection("Users:Outbox"));
        services.ConfigureOptions<ConfigureProcessOutboxJob>();
    }
}
