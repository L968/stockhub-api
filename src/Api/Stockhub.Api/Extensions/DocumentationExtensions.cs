using Scalar.AspNetCore;
using Stockhub.Common.Presentation;

namespace Stockhub.Api.Extensions;

internal static class DocumentationExtensions
{
    public static IServiceCollection AddDocumentation(this IServiceCollection services)
    {
        foreach (int version in ApiVersions.Versions)
        {
            services.AddOpenApi($"v{version}");
        }

        return services;
    }

    public static IApplicationBuilder UseDocumentation(this WebApplication app)
    {
        app.MapOpenApi();

        app.MapScalarApiReference(options => {
            options
                .WithTitle("Stockhub Api")
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

            options.Servers = [];
        });

        return app;
    }
}
