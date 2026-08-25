using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AzureArchitect.Extensions;
using AzureArchitect.Facade;
using AzureArchitect.Services.BlobStorage;
using AzureServices.Entity;
using AzureServices.Enums;
using AzureServices.Facade;
using AzureServices.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
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
                            //services.AddStorageLibrary(context.Configuration);
                        }
                        catch (Exception ex)
                        {
                            Console.Write(ex.Message);
                            throw new Exception(ex.Message);
                        }
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

#region Service Bus

//var serviceBusService = host.Services.GetRequiredService<IServiceBusService>();
//var messagingService = host.Services.GetRequiredService<IMessagingService>();

#region Admin

//await messagingService.CreateQueueAsync(queueName);
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

#endregion Service Bus

#region Storage

//var storageService = host.Services.GetRequiredService<IBlobService>();
try
{
    var blobName = "sample.txt";
    var containerName = "createdfromcode";
    var localPath = @"C:\temp\sample.txt";

    //Upload a file
    //await using (var fs = File.OpenRead(localPath))
    //{
    //    await storageService.UploadAsync(containerName, blobName, fs);
    //}

    //var fileList = new List<FileToUpload>();

    //var fs = File.OpenRead(localPath);

    //fileList.Add(new FileToUpload() { FileName = Path.GetFileName(localPath), Content = fs });
    //await fs.FlushAsync();

    //fs = File.OpenRead(@"C:\temp\sample_downloaded.txt");

    //fileList.Add(new FileToUpload() { FileName = Path.GetFileName(@"C:\temp\sample_downloaded.txt"), Content = fs });
    //await fs.FlushAsync();

    //await storageService.BulkUploadAsync(containerName, fileList);

    //Upload from file path
    //await storageService.UploadFromFileAsync(containerName, blobName, localPath);

    //await storageService.BulkUploadFromFileAsync(containerName, new List<string> { @"C:\temp\sample.txt", @"C:\temp\sample_downloaded.txt" });

    //logger.LogInformation("File uploaded.");

    //long minutes = 1;

    ////Generate SAS Url
    //var sasUri = await storageService.GenerateBlobSasUri(containerName, blobName, TimeSpan.FromMinutes(minutes));
    //var sasUri = await storageService.GenerateUserDelegationSasUriAsync(containerName, blobName, TimeSpan.FromMinutes(minutes));

    //logger.LogInformation($"Blob SAS URI (valid {minutes} minutes): {sasUri}");

    // Download to a file
    //await storageService.DownloadToFileAsync(containerName, blobName, @"C:\temp\sample_downloaded4.txt");

    //logger.LogInformation("File downloaded.");

    // Delete blob
    //await storageService.DeleteAsync(containerName, "sample3.txt");
    //logger.LogInformation("File deleted.");

    //var exist = await storageService.GetPropertiesAsync(containerName, "sample.txt");
    //Console.WriteLine($"{exist.ContentLength} bytes");
    //foreach (var md in exist.Metadata)
    //{
    //    logger.LogInformation($"{md.Key} : {md.Value}");
    //}

    //IDictionary<string, string?> metadata=new Dictionary<string, string?>();
    //metadata.Add("md", "789");
    //metadata.Add("test", "123");
    //await storageService.SetMetadataAsync(containerName, "sample.txt", metadata!);
    //logger.LogInformation("Metadata set");

    //var blobs = await storageService.ListBlobsAsync(containerName);
    //foreach (var blob in blobs)
    //{
    //    Console.WriteLine($"{blob.Name} : {blob.Properties.ContentLength} bytes");
    //}

    //Copy from URL
    //var copyID = await storageService.StartCopyFromUriAsync(containerName, "AllanDonald.jpg", new Uri("https://www.sporting-heroes.net/content/thumbnails/00013/01186-zoom.jpg"));

    //Console.WriteLine($"File copied {copyID}");
}
catch (Exception ex)
{
    logger.LogError(ex.Message);
}

#endregion Storage

// keep the host alive so processors run
await host.RunAsync();