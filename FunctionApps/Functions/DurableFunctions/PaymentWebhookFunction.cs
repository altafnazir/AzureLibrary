using Azure.Core;
using FunctionApps.Functions.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

//Webhook that raise event PaymentReceived for StartOrderFunction flow
namespace FunctionApps.DurableFunction
{
    public class PaymentWebhookFunction
    {
        private readonly ILogger<PaymentWebhookFunction> _logger;

        public PaymentWebhookFunction(ILogger<PaymentWebhookFunction> logger)
        {
            _logger = logger;
        }

        [Function("PaymentWebhook")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] 
                                                HttpRequestData request,
                                                [DurableClient] DurableTaskClient client)
        {
            _logger.LogInformation("PaymentWebhook called.");

            var body = await request.ReadFromJsonAsync<PaymentModel>();

            if (body != null)
            {
                await client.RaiseEventAsync(
                    body.InstanceId,
                    "PaymentReceived",
                    body.PaymentId);
            }

            return new OkObjectResult("PaymentWebhook completed!");
        }
    }
}
