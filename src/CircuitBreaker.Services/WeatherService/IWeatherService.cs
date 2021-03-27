using System.Threading.Tasks;
using CircuitBreaker.Services.WeatherService.Models;
using OneOf;

namespace CircuitBreaker.Services.WeatherService
{
    public interface IWeatherService
    {
        Task<OneOf<OpenWeatherResponse, SecondaryWeatherResponse, NotFound, WeatherServiceException>> GetWeather(string location);
    }
}