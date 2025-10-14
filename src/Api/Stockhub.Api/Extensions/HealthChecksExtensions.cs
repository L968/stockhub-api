using Stockhub.Common.Infrastructure;
using Stockhub.Common.Infrastructure.Extensions;

namespace Stockhub.Api.Extensions;

internal static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionStringOrThrow(ServiceNames.PostgresDb))
            .AddRedis(configuration.GetConnectionStringOrThrow(ServiceNames.Redis));

        return services;
    }
}
