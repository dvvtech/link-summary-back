using LinkSummary.Api.BLL.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace LinkSummary.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        private readonly IAiClient _aiClient;

        public WeatherForecastController(
            IAiClient aiClient,
            ILogger<WeatherForecastController> logger)
        {
            _aiClient = aiClient;
            _logger = logger;
        }

        [HttpGet("test")]
        public async Task<string> Test()
        {
            _logger.LogInformation("call test");
            var res = await _aiClient.GetTextResponseAsync("напиши четырехстишье про природу", "ты профессиональный писатель");
            return res;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
