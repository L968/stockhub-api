using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stockhub.Common.Infrastructure;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Common.Presentation.Endpoints;
using Stockhub.Modules.Stocks.Application.Abstractions;
using Stockhub.Modules.Stocks.Infrastructure.Database;
using Stockhub.Modules.Stocks.Infrastructure.PublicApi;
using Stockhub.Modules.Stocks.PublicApi;

namespace Stockhub.Modules.Stocks.Infrastructure;

public static class StocksModule
{
    public static IServiceCollection AddStocksModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);
        services.AddScoped<IStocksApi, StocksApi>();

        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string dbConnectionString = configuration.GetConnectionStringOrThrow(ServiceNames.PostgresDb);

        services.AddDbContext<StocksDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(
                    connectionString: dbConnectionString,
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, ServiceNames.DatabaseName)
                )
        );

        services.AddScoped<IStocksDbContext>(sp => sp.GetRequiredService<StocksDbContext>());
    }
}
