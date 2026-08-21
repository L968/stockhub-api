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
using Stockhub.Modules.Users.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

Assembly[] moduleApplicationAssemblies = [
    Stockhub.Modules.Orders.Application.AssemblyReference.Assembly,
    Stockhub.Modules.Stocks.Application.AssemblyReference.Assembly,
    Stockhub.Modules.Users.Application.AssemblyReference.Assembly
];

builder.Services.AddApplication(moduleApplicationAssemblies);

builder.Configuration.AddModuleConfiguration(["orders", "stocks", "users"]);

builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddStocksModule(builder.Configuration);
builder.Services.AddUsersModule(builder.Configuration);

builder.Services.AddHealthChecksConfiguration(builder.Configuration);

builder.Services.AddDocumentation();

builder.Services.AddVersioning();

builder.Host.AddSerilogLogging();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Policy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) &&
              (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Equals("127.0.0.1", StringComparison.Ordinal)))
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();

app.MapDefaultEndpoints();

app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDocumentation();
}

app.UseExceptionHandler(o => { });

app.UseCors("Policy");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

await app.RunAsync();
