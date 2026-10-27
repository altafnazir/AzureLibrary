using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;

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

        [HttpGet(Name = "GetWeatherForecast")]
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
    }
}
