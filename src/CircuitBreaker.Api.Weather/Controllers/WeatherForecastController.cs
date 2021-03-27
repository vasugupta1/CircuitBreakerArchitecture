using System;
using System.Threading.Tasks;
using CircuitBreaker.Services.WeatherService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CircuitBreaker.Api.Weather.Controllers
{
    [ApiController]
    [Route("")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IWeatherService _weatherService;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherService weatherService)
        {
            _logger = logger;
            _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
        }

        [HttpGet("weather/{location}")]
        public async Task<IActionResult> GetWeather(string location)
        {
            if(string.IsNullOrEmpty(location))
            {
                throw new ArgumentNullException(nameof(location));
            }

            try
            {
                var oneOfResult = await _weatherService.GetWeather(location);

                return oneOfResult.Match<IActionResult>(
                    OpenWeatherResponse => new OkObjectResult(OpenWeatherResponse.main.Temp),
                    SecondaryWeatherResponse => new OkObjectResult(SecondaryWeatherResponse.Current.TempC),
                    NotFound => new NotFoundObjectResult("Weather was not found for the given location"),
                    WeatherServiceException => new StatusCodeResult(500));
            }
            catch(Exception ex)
            {
                _logger.LogError("Error when getting weather", ex);
                return new StatusCodeResult(500);
            }
        }
    }
}
