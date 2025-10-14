using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;
using StackExchange.Redis;
using Stockhub.Common.Application.Caching;
using Stockhub.Common.Infrastructure.Caching;
using Stockhub.Common.Infrastructure.Outbox;

namespace Stockhub.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructure(this IServiceCollection services, string redisConnectionString)
    {
        services.TryAddSingleton<InsertOutboxMessagesInterceptor>();

        services.AddRedis(redisConnectionString);

        services.AddQuartz();
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    }

    private static void AddRedis(this IServiceCollection services, string redisConnectionString)
    {
        try
        {
            IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            services.AddSingleton(connectionMultiplexer);
            services.AddStackExchangeRedisCache(options =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer));
        }
        catch
        {
            services.AddDistributedMemoryCache();
        }

        services.TryAddSingleton<ICacheService, CacheService>();
    }
}
