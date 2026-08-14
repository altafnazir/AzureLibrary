
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureArchitect.Extensions;
using AzureArchitect.Facade;
using AzureArchitect.Services;
using AzureServices.Entity;
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
    try
    {
        builder.Services.AddServiceBusLibrary(builder.Configuration);
    }
    catch (Exception ex)
    {
        Console.Write(ex.Message);
        throw new Exception(ex.Message);
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
