using Azure.Core;
using FunctionApps.Functions.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace FunctionApps.Functions.DurableFunctions;

//Monitor pattern
public static class InventoryMonitorFunction
{
    [Function("StartOrderInventoryMonitor")]
    public static async Task<HttpResponseData> StartOrderInventoryMonitor(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("StartOrderInventoryMonitor");
        var model =
            await req.ReadFromJsonAsync<InventoryMonitorRequest>();

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            "InventoryMonitorOrchestrator", model);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }

    [Function("InventoryMonitorOrchestrator")]
    public static async Task InventoryMonitorOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger("InventoryMonitorOrchestrator");
        logger.LogInformation("InventoryMonitorOrchestrator called.");

        var request =
            context.GetInput<InventoryMonitorRequest>();

        if (request == null)
        {
            logger.LogInformation("Invalid request data");
            return;
        }

        var expiry =
            context.CurrentUtcDateTime.AddHours(24);

        while (context.CurrentUtcDateTime < expiry)
        {
            bool available =
                await context.CallActivityAsync<bool>(
                    "CheckInventoryActivity",
                    request.ProductId);

            if (available)
            {
                await context.CallActivityAsync(
                    "SendEmailActivity",
                    request);

                return;
            }

            var nextCheck =
                context.CurrentUtcDateTime.AddSeconds(10);

            await context.CreateTimer(
                nextCheck,
                CancellationToken.None);
        }
    }

    [Function("CheckInventoryActivity")]
    public static async Task<bool> CheckInventoryActivity([ActivityTrigger] int productId, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("CheckInventoryActivity");
        logger.LogInformation("CheckInventoryActivity called");

        await Task.CompletedTask;

        return true;
    }

    [Function("SendEmailActivity")]
    public static async Task SendEmailActivity([ActivityTrigger] InventoryMonitorRequest inventoryMonitorRequest, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("SendEmailActivity");
        logger.LogInformation("SendEmailActivity called");

        await Task.CompletedTask;
    }
}