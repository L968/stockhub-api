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
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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

app.UseHttpsRedirection();

app.UseCors("Policy");

await app.RunAsync();
