using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using AzureArchitect.Extensions;
using AzureArchitect.Facade;
using AzureServices.Entity;
using AzureServices.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
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
                        try
                        {
                            services.AddServiceBusLibrary(context.Configuration);
                        }
                        catch (Exception ex)
                        {
                            Console.Write(ex.Message);
                            throw new Exception(ex.Message);
                        }

                        //var serviceBusConfiguration = context.Configuration
                        //                .GetSection("ServiceBus")
                        //                .Get<ServiceBusConfiguration>();

                        //if (serviceBusConfiguration == null)
                        //{
                        //    throw new InvalidOperationException(
                        //        "Missing or malformed configuration section 'ServiceBus'. " +
                        //        "Ensure appsettings.json contains a valid ServiceBus section with required properties.");
                        //}
                        //try
                        //{
                        //    services.AddServiceBusLibrary(serviceBusConfiguration);
                        //}
                        //catch (Exception ex)
                        //{
                        //    Console.Write(ex.Message);
                        //    throw new Exception(ex.Message);
                        //}
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

//var vaultUri = "https://learnkeyvault2026.vault.azure.net/";

//if (string.IsNullOrWhiteSpace(vaultUri)) throw new ArgumentNullException(nameof(vaultUri));

// Use DefaultAzureCredential but avoid Managed Identity probing when running locally.

//var options = new DefaultAzureCredentialOptions
//{
//    ExcludeManagedIdentityCredential = true
//};
//DefaultAzureCredential credential = new DefaultAzureCredential(options);

//var _secretClient = new SecretClient(new Uri(vaultUri), credential);
//var _keyClient = new KeyClient(new Uri(vaultUri), credential);
//var secretResponse = _secretClient.GetSecret("servicebus");
//var secretValue = secretResponse?.Value?.Value;
//AsyncPageable<SecretProperties> allSecrets = _secretClient.GetPropertiesOfSecretsAsync();

//await _secretClient.SetSecretAsync("servicebusconstr", "Endpoint=sb://learnservicebus2026.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dY6aUDt0OgrCIyDn4CdVlzn5yzCiPr1YZ+ASbETySxc=");
//Console.WriteLine("Key added");

//await foreach (SecretProperties secretProperties in allSecrets)
//{
//    Console.WriteLine(secretProperties.Name);
//}

var serviceBusService = host.Services.GetRequiredService<IServiceBusService>();
var messagingService = host.Services.GetRequiredService<IMessagingService>();

#region Admin

await messagingService.CreateQueueAsync(queueName);
//await messagingService.CreateTopicAsync(topicName);

//foreach (var vd in validDepartments)
//{
//    await messagingService.CreateSubscriptionAsync(topicName, $"{vd}Subscription", $"Department = '{vd}' AND ValidDepartment = true");
//}

//await messagingService.CreateSubscriptionAsync(topicName, "InvalidDepartment", "ValidDepartment = false");

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

ServiceBusData message = new ServiceBusData
{
    Id = 2,
    Department = "HR",
    Title = "Welcome",
    Body = $"This is test message."
};

var messageJson = JsonSerializer.Serialize(message);
for (int i = 0; i < 5; i++)
{
    await messagingService.SendMessageAsync(queueName, messageJson);
}

Console.WriteLine($"Message sent to queue: {queueName}");

#endregion Send

#region Receive

//Pull receive

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