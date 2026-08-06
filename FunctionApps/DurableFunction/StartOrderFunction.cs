using FunctionApps.Functions.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace FunctionApps.DurableFunction
{
    public static class StartOrderFunction
    {
        [Function("StartOrder")]
        public static async Task<HttpResponseData> StartOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("StartOrder");

            var order = await req.ReadFromJsonAsync<OrderRequest>();

            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("OrderOrchestrator", order);

            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }

        [Function("OrderOrchestrator")]
        public static async Task OrderOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger("OrderOrchestrator");
            logger.LogInformation("Order received.");

            var order =
                context.GetInput<OrderRequest>();

            await context.CallActivityAsync(
                "ReserveInventory",
                order);

            var timeout =
                context.CurrentUtcDateTime.AddMinutes(30);

            var timeoutTask =
                context.CreateTimer(
                    timeout,
                    CancellationToken.None);

            var paymentTask =
                context.WaitForExternalEvent<bool>(
                    "PaymentReceived");

            var winner =
                await Task.WhenAny(
                    paymentTask,
                    timeoutTask);

            if (winner == paymentTask)
            {
                await context.CallActivityAsync(
                    "ShipOrder",
                    order);
            }
            else
            {
                await context.CallActivityAsync(
                    "ReleaseInventory",
                    order);
            }
        }

        [Function("ReserveInventory")]
        public static async Task ReserveInventory([ActivityTrigger] OrderRequest order, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("ReserveInventory");
            logger.LogInformation("ReserveInventory called.");

            await Task.CompletedTask;
        }

        [Function("ShipOrder")]
        public static async Task ShipOrder([ActivityTrigger] OrderRequest order, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("ShipOrder");
            logger.LogInformation("ShipOrder called.");

            await Task.CompletedTask;
        }

        [Function("ReleaseInventory")]
        public static async Task ReleaseInventory([ActivityTrigger] OrderRequest order, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("ReleaseInventory");
            logger.LogInformation("ReleaseInventory called.");

            await Task.CompletedTask;
        }
    }
}
