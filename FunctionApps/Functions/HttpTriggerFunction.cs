using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApps.Functions
{
    public class HttpTriggerFunction
    {
        private readonly ILogger<HttpTriggerFunction> _logger;

        public HttpTriggerFunction(ILogger<HttpTriggerFunction> logger)
        {
            _logger = logger;
        }

        [Function("GetDetails")]
        public IActionResult GetDetails([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "details/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("Getting details for {Id}", id);
            return new OkObjectResult("Details for " + id);
        }
    }
}
