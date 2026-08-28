using AzureArchitect.Extensions;
using Microsoft.OpenApi.Models;

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
    // Create a logger factory for startup-time logging (before the app is built)
    using var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
    var logger = loggerFactory.CreateLogger("Program");

    try
    {
        logger.LogInformation("Configuring Service Bus library...");
        builder.Services.AddServiceBusLibrary(builder.Configuration);
        //builder.Services.AddStorageLibrary(builder.Configuration);
        logger.LogInformation("Service Bus library configured successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to configure Service Bus library.");
        throw;
    }

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
