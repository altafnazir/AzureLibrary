using EntraIDWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System.Buffers.Text;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace EntraIDWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger,
                                ITokenAcquisition tokenAcquisition,
                                IHttpClientFactory httpClientFactory,
                                IConfiguration configuration)
        {
            _logger = logger;
            _tokenAcquisition = tokenAcquisition;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //Called using Deleted permission
        [Authorize]
        [RequiredScope("api.read")]
        public async Task<IActionResult> GetOrders()
        {
            string scope = _configuration["OrderApi:Scope"]!;
            string baseUrl = _configuration["OrderApi:BaseUrl"]!;

            string accessToken =
                await _tokenAcquisition
                    .GetAccessTokenForUserAsync(new[] { scope });

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer", accessToken);

            var response = await client.GetAsync(
                $"{baseUrl.TrimEnd('/')}/api/WeatherForecast/getorders");

            Console.WriteLine($"Requested URL: {client.BaseAddress}");
            Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"Final URL: {response.RequestMessage?.RequestUri}");
            Console.WriteLine(await response.Content.ReadAsStringAsync());

            var content =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, content);
            }

            return Content(content, "application/json");
        }
    }
}
