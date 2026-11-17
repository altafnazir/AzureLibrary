using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System.Text.Json;

namespace DummyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            using var client = new HttpClient();

            var ip = client.GetStringAsync("https://api.ipify.org").Result;

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = "IP Address is: " + ip
            })
            .ToArray();
        }

        [HttpGet("WeatherForecastV2")]
        public IEnumerable<WeatherForecastV2> WeatherForecastV2()
        {
            using var client = new HttpClient();

            var ip = client.GetStringAsync("https://api.ipify.org").Result;

            return Enumerable.Range(1, 5).Select(index => new WeatherForecastV2
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Unit = "Celsius",
                Summary = "IP Address is: " + ip
            })
            .ToArray();
        }

        [HttpGet("myinfo")]
        public IEnumerable<dynamic> GetMyInfo()
        {
            var info = new List<dynamic>
            {
                new
                {
                    Name="Altaf Patel",
                    Post="Technical Lead",
                    TechStack=".NET"
                }
            };

            return info;
        }

        [Authorize]
        [HttpGet("getorders")]
        [RequiredScope("api.read")]
        //[Authorize(Roles = "Orders.Read.All")]
        public IActionResult GetOrders()
        {
            return Ok(new[]
            {
            new { Id = 1, Product = "Laptop" },
            new { Id = 2, Product = "Keyboard" }
        });
        }

        [HttpGet("CheckHeaders")]
        public IActionResult CheckHeaders()
        {
            var source = Request.Headers["X-Source"].ToString();

            return Ok(new
            {
                ReceivedHeader = source,
                Response = source
            });
        }

        [HttpGet("getitems")]
        public IActionResult GetItems()
        {
            return Ok(new[]
            {
            new { Id = 1, Product = "Laptop" },
            new { Id = 2, Product = "Keyboard" }
        });
        }
        [HttpGet("PlainText")]
        public IActionResult PlainText()
        {
            return Content("Operation successful", "text/plain");
        }
        [HttpGet("EmptyResponse")]
        public IActionResult EmptyResponse()
        {
            return NoContent();
        }
        [HttpGet("MalformedJson")]
        public IActionResult MalformedJson()
        {
            return Content(
                "{\"name\":\"Order\",\"price\":",
                "application/json"
            );
        }
        [HttpPost("TransformRequest")]
        public IActionResult TransformRequest([FromBody] JsonElement request)
        {
            return Ok(request);
        }
        [HttpGet("XmlOrder")]
        public IActionResult XmlOrder()
        {
            var xml = """
                        <Order>
                            <OrderId>101</OrderId>
                            <Customer>John</Customer>
                            <Product>Laptop</Product>
                            <Price>50000</Price>
                        </Order>
                        """;

            return Content(xml, "application/xml");
        }
        [HttpGet("DelayedResponse")]
        public async Task<IActionResult> DelayedResponse()
        {
            await Task.Delay(TimeSpan.FromSeconds(40));

            return Ok(new
            {
                message = "Response received after 40 seconds"
            });
        }
        private static int _retryAttempt = 0;

        [HttpGet("RetryTest")]
        public IActionResult RetryTest()
        {
            int attempt = Interlocked.Increment(ref _retryAttempt);

            if (attempt <= 2)
            {
                return StatusCode(503, new
                {
                    attempt,
                    message = "Temporary service unavailable"
                });
            }

            return Ok(new
            {
                attempt,
                message = "Service recovered successfully"
            });
        }
    }
}
