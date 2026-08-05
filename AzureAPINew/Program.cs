
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureArchitect.Extensions;
using AzureArchitect.Facade;
using AzureArchitect.Services;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AzureAPI",
        Version = "v1",
        Description = "Swagger UI for AzureAPI"
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
{
    // Resolve connection string from multiple locations (same logic as your snippet)
    var connectionString =
        builder.Configuration["ServiceBus:ConnectionString"]
        ?? builder.Configuration.GetConnectionString("ServiceBus")
        ?? builder.Configuration["ServiceBusConnectionString"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Service Bus connection string not found in configuration. Check ServiceBus:ConnectionString or ConnectionStrings:ServiceBus.");
    }

    // Read retry options from configuration if present, otherwise use defaults
    var retryOptions = builder.Configuration
        .GetSection("ServiceBus:ClientOptions:RetryOptions")
        .Get<ServiceBusRetryOptions>() ?? new ServiceBusRetryOptions();

    var clientOptions = new ServiceBusClientOptions { RetryOptions = retryOptions };

    // Read processor options (used when you create processors elsewhere)
    var serviceBusProcessorOptions = builder.Configuration
        .GetSection("ServiceBus:ProcessorOptions")
        .Get<ServiceBusProcessorOptions>() ?? new ServiceBusProcessorOptions();

    // Register your library extension (keeps existing behavior)
    builder.Services.AddServiceBusLibrary(builder.Configuration);

    // Register ServiceBusClient and processor options for DI consumers
    builder.Services.AddSingleton(new ServiceBusClient(connectionString, clientOptions));
    builder.Services.AddSingleton(new ServiceBusAdministrationClient(connectionString));
    builder.Services.AddSingleton(serviceBusProcessorOptions);

    // If your ServiceBusService depends on IServiceBusService, register it
    builder.Services.AddSingleton<ServiceBusService>();

    builder.Services.AddSingleton<IMessagingService>(
        sp => sp.GetRequiredService<ServiceBusService>());

    builder.Services.AddSingleton<IServiceBusService>(
        sp => sp.GetRequiredService<ServiceBusService>());

    Azure.Core.Diagnostics.AzureEventSourceListener.CreateConsoleLogger();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AzureAPI v1");
        // To host the UI at the app root, uncomment the next line:
        // c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();
