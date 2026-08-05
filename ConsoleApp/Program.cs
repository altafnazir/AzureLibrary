using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureArchitect.Extensions;
using AzureArchitect.Facade;
using AzureServices.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

var queueName = "message-queue";
var topicName = "events-topic";
HashSet<string> validDepartments = new() { "HR", "IT", "Finance" };

using IHost host = Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        var connectionString =
                            context.Configuration["ServiceBus:ConnectionString"]
                            ?? context.Configuration.GetConnectionString("ServiceBus")
                            ?? context.Configuration["ServiceBusConnectionString"];

                        if (string.IsNullOrWhiteSpace(connectionString))
                            throw new InvalidOperationException("Service Bus connection string is not configured. Set ServiceBus:ConnectionString or ConnectionStrings:ServiceBus in appsettings.json.");

                        var retryOptions = context.Configuration
                                                    .GetSection("ServiceBus:RetryOptions")
                                                    .Get<ServiceBusRetryOptions>() ?? new ServiceBusRetryOptions();

                        var clientOptions = new ServiceBusClientOptions { RetryOptions = retryOptions };

                        var serviceBusProcessorOptions = context.Configuration
                            .GetSection("ServiceBus:ProcessorOptions")
                            .Get<ServiceBusProcessorOptions>() ?? new ServiceBusProcessorOptions();

                        services.AddServiceBusLibrary(context.Configuration);

                        services.AddSingleton(new ServiceBusClient(connectionString, clientOptions));
                        services.AddSingleton(new ServiceBusAdministrationClient(connectionString));
                        services.AddSingleton(serviceBusProcessorOptions);
                    })
                    .ConfigureLogging((context, logging) =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Debug);
                        logging.AddConsole();
                        logging.AddFilter("Azure", LogLevel.Debug);
                        logging.AddFilter("Azure.Messaging.ServiceBus", LogLevel.Debug);

                        // Capture Azure SDK logs
                        //Azure.Core.Diagnostics.AzureEventSourceListener.CreateConsoleLogger();
                    })
                    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Host built; ServiceBus client configured.");

var serviceBusService = host.Services.GetRequiredService<IServiceBusService>();
var messagingService = host.Services.GetRequiredService<IMessagingService>();

#region Admin

await messagingService.CreateQueueAsync(queueName);
await messagingService.CreateTopicAsync(topicName);

foreach (var vd in validDepartments)
{
    await messagingService.CreateSubscriptionAsync(topicName, $"{vd}Subscription", $"Department = '{vd}' AND ValidDepartment = true");
}

await messagingService.CreateSubscriptionAsync(topicName, "InvalidDepartment", "ValidDepartment = false");

#endregion Admin

#region Topic

var testDepartment = new[] { "HR", "IT", "Finance" };

#region Send

//foreach (var dep in testDepartment)
//{
//    await SendMessageAsync(dep, topicName);
//}

//Console.WriteLine($"{testDepartment.Count()} Messages sent to topic: {topicName}");

//async Task SendMessageAsync(string department, string topicName)
//{
//    string messageJson;
//    bool validDepartment = false;
//    ServiceBusData message;

//    message = new ServiceBusData
//    {
//        Id = 2,
//        Department = department,
//        Title = "Welcome",
//        Body = $"This message is for subscription {department}."
//    };

//    if (validDepartments.Any(dept => dept == department))
//    {
//        validDepartment = true;
//    }

//    messageJson = JsonSerializer.Serialize(message);

//    await messagingService.SendMessageAsync(topicName, messageJson,
//                                        new Dictionary<string, object>
//                                        {
//                                        { "Department", message.Department },
//                                        { "ValidDepartment", validDepartment }
//                                        });
//}

#endregion Send

#region Receive

//Pull Receive

//foreach (var dep in testDepartment)
//{
//    await serviceBusService.StartProcessorAsync(
//    topicName,
//    $"{dep}Subscription",
//    async args =>
//    {
//        var body = args.Message.Body.ToString();
//        Console.WriteLine($"Received: {body}");
//        await args.CompleteMessageAsync(args.Message);
//    },
//    async errorArgs =>
//    {
//        Console.WriteLine($"Processor error: {errorArgs.Exception}");
//        await Task.CompletedTask;
//    }
//    );
//}

#endregion  Receive

#endregion Topic

#region Queue

#region Send

//ServiceBusData message = new ServiceBusData
//{
//    Id = 2,
//    Department = "HR",
//    Title = "Welcome",
//    Body = $"This is test message."
//};

//var messageJson = JsonSerializer.Serialize(message);
//for (int i = 0; i < 5; i++)
//{
//    await messagingService.SendMessageAsync(queueName, messageJson);
//}

//Console.WriteLine($"Message sent to queue: {queueName}");

//#endregion Send

//#region Receive

////Pull receive

//await serviceBusService.StartQueueProcessorAsync(
//queueName,
//async args =>
//{
//    var body = args.Message.Body.ToString();
//    Console.WriteLine($"Received: {body}");
//    await args.CompleteMessageAsync(args.Message);
//},
//async errorArgs =>
//{
//    Console.WriteLine($"Processor error: {errorArgs.Exception}");
//    await Task.CompletedTask;
//}
//);

#endregion Receive

#endregion Queue

// keep the host alive so processors run
await host.RunAsync();
// or: await Task.Delay(Timeout.Infinite);