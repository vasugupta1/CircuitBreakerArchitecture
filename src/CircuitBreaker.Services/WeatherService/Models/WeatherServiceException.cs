using System;

namespace CircuitBreaker.Services.WeatherService.Models
{
    public class WeatherServiceException : Exception
    {
        public int StatusCode {get;set;}
        public WeatherServiceException(string message, Exception innerException, int statusCode) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}