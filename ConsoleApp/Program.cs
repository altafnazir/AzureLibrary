using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureArchitect.Common;
using AzureArchitect.Extensions;
using AzureArchitect.Facade;
using AzureArchitect.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
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

var serviceBus = host.Services.GetRequiredService<IServiceBusService>();

#region Admin

await serviceBus.CreateQueueAsync(queueName);
await serviceBus.CreateTopicAsync(topicName);

foreach (var vd in validDepartments)
{
    await serviceBus.CreateSubscriptionAsync(topicName, $"{vd}Subscription", $"Department = '{vd}' AND ValidDepartment = true");
}

await serviceBus.CreateSubscriptionAsync(topicName, "InvalidDepartment", "ValidDepartment = false");

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

//    await serviceBus.SendMessageAsync(topicName, messageJson,
//                                        new Dictionary<string, object>
//                                        {
//                                        { "Department", message.Department },
//                                        { "ValidDepartment", validDepartment }
//                                        });
//}

#endregion Send

#region Receive

//foreach (var dep in testDepartment)
//{
//    var subMessage = await serviceBus.ReceiveSubscriptionMessageAsync(topicName, $"{dep}Subscription");

//    var messageObject = JsonSerializer.Deserialize<ServiceBusData>(subMessage!);

//    Console.WriteLine($"Message received from topic {topicName}, subscription {dep}Subscription:\n {messageObject?.Title}: {messageObject?.Body}");
//}

//Continuous pull

//foreach (var dep in testDepartment)
//{
//    await serviceBus.StartProcessorAsync(
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
//    await serviceBus.SendMessageAsync(queueName, messageJson);
//}

//Console.WriteLine($"Message sent to queue: {queueName}");

#endregion Send

#region Receive

//string? queueMsg = await serviceBus.ReceiveMessageAsync(queueName);

//var messageObject = JsonSerializer.Deserialize<ServiceBusMessage>(queueMsg!);

//Console.WriteLine($"Message received from queue {queueName}:\n {messageObject?.Title}: {messageObject?.Body}");

//Continuous pull

//await serviceBus.StartQueueProcessorAsync(
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