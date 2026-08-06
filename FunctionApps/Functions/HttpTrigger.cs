using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApps.Functions
{
    public class HttpTrigger
    {
        private readonly ILogger<HttpTrigger> _logger;

        public HttpTrigger(ILogger<HttpTrigger> logger)
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
