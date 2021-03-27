using System.Text.Json;
using System.Text.Json.Serialization;

namespace CircuitBreaker.Services.WeatherService.Models
{
    public class OpenWeatherResponse
    {
        [JsonPropertyName("main")]
        public Main main{get;set;}
    }

    public class Main
    {
        [JsonPropertyName("temp")]
        public decimal Temp{get;set;}
    }
}