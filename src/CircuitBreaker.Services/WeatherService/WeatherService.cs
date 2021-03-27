using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CircuitBreaker.Services.WeatherService.Models;
using OneOf;
using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;

namespace CircuitBreaker.Services.WeatherService
{
    public class WeatherService : CircuitBreaker.Services.WeatherService.IWeatherService
    {
        ///
        //If the reponse from the api is 429 or anything above 500 then we want to retry for total of 10 times wih 
        ///
        private static readonly AsyncPolicy<HttpResponseMessage> TransientErrorRetryPolicy = 
        Policy
        .HandleResult<HttpResponseMessage>(message => (int)message.StatusCode == 429 || (int)message.StatusCode >= 500)
        .WaitAndRetryAsync(10, retryAttempt => 
        {
            Console.WriteLine($"Retrying because of transient error. Attempt : {retryAttempt}");
            return TimeSpan
            .FromMinutes(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(new Random().Next(1,3)*1000);
        });

        ///
        // If the api returns 503, 50% of the time out of 100 request in a min then open circuit
        ///
        private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> CircuitBreakerPolicy = 
        Policy
        .HandleResult<HttpResponseMessage>( message => (int)message.StatusCode == 503)
        .AdvancedCircuitBreakerAsync(0.5, TimeSpan.FromMinutes(1), 100, TimeSpan.FromMinutes(1));
    
        ///
        // Wrap CircuitBreakerPolicy (outer) with TransientErrorRetryPolicy (inner)
        ///
        private AsyncPolicyWrap<HttpResponseMessage> _resilientPolicy = CircuitBreakerPolicy.WrapAsync(TransientErrorRetryPolicy);

        private readonly HttpClient _openWeatherApiClient;
        private readonly HttpClient _weatherApiClient;
        private readonly IReadOnlyDictionary<string,string> _apiKeys;

        public WeatherService(HttpClient openWeatherApiClient, HttpClient weatherApiClient, IReadOnlyDictionary<string,string> apiKeys)
        {
            _weatherApiClient = weatherApiClient ?? throw new ArgumentNullException(nameof(weatherApiClient));
            _openWeatherApiClient = openWeatherApiClient ?? throw new ArgumentNullException(nameof(openWeatherApiClient));
            _apiKeys = apiKeys ?? throw new ArgumentNullException(nameof(apiKeys));
        }

         public async Task<OneOf<OpenWeatherResponse, SecondaryWeatherResponse, NotFound, WeatherServiceException>> GetWeather(string location)
        {
            if(string.IsNullOrEmpty(location))
            {
                throw new ArgumentException("Parameter cannot be null or empty", location);
            }
            try
            {
                if(CircuitBreakerPolicy.CircuitState == CircuitState.Open)
                {
                    var secondaryApiResponse =  await CallSecondaryApi(location);
                    if(secondaryApiResponse is null)
                    {
                        return new NotFound();
                    }
                    return secondaryApiResponse;
                }

                var response = await _resilientPolicy.ExecuteAsync(
                    ()=> _openWeatherApiClient
                    .GetAsync($"data/2.5/weather?q={location}&appid={_apiKeys[WeatherApis.OpenWeatherApi]}"));

                var responseObject = JsonSerializer.Deserialize<OpenWeatherResponse>(await response.Content.ReadAsStringAsync());

                if(responseObject is null)
                {
                    return new NotFound();
                }

                return responseObject;
            }
            catch(Exception ex)
            {
                return new WeatherServiceException("Failed for some reason, check inner exception", ex, 500);
            }
        }

        private async Task<SecondaryWeatherResponse> CallSecondaryApi(string location)
        {
            var weatherApiResponse = await _weatherApiClient.GetAsync($"current.json?key={WeatherApis.WeatherApi}&q={location}&aqi=no");
            var weatherApiObject = JsonSerializer.Deserialize<SecondaryWeatherResponse>(await weatherApiResponse.Content.ReadAsStringAsync());         
            if(weatherApiObject is null)
            {
                return null;
            }
            return weatherApiObject;
        }
    }
}