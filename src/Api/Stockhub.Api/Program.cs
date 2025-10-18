using System.Reflection;
using Serilog;
using Stockhub.Api.Extensions;
using Stockhub.Api.Middleware;
using Stockhub.Aspire.ServiceDefaults;
using Stockhub.Common.Application;
using Stockhub.Common.Infrastructure;
using Stockhub.Common.Infrastructure.Extensions;
using Stockhub.Common.Presentation.Endpoints;
using Stockhub.Modules.Orders.Infrastructure;
using Stockhub.Modules.Stocks.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

Assembly[] moduleApplicationAssemblies = [
    Stockhub.Modules.Orders.Application.AssemblyReference.Assembly,
    Stockhub.Modules.Stocks.Application.AssemblyReference.Assembly
];

builder.Services.AddApplication(moduleApplicationAssemblies);

string redisConnectionString = builder.Configuration.GetConnectionStringOrThrow(ServiceNames.Redis);

builder.Services.AddInfrastructure(redisConnectionString);

builder.Configuration.AddModuleConfiguration(["orders, stocks"]);

builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddStocksModule(builder.Configuration);

builder.Services.AddHealthChecksConfiguration(builder.Configuration);

builder.Services.AddDocumentation();

builder.Services.AddVersioning();

builder.Host.AddSerilogLogging();

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();

app.MapDefaultEndpoints();

app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDocumentation();
}

app.UseExceptionHandler(o => { });

app.UseHttpsRedirection();

await app.RunAsync();
