using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using CircuitBreaker.Services.WeatherService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CircuitBreaker.Api.Weather
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        { 
            services.AddControllers();
            services.AddHttpClient(CircuitBreaker.Services.WeatherService.Models.WeatherApis.OpenWeatherApi, client => 
            {
                client.BaseAddress = new Uri(Configuration.GetValue<string>("OpenWeatherApiBaseUrl"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            services.AddHttpClient(CircuitBreaker.Services.WeatherService.Models.WeatherApis.WeatherApi, client => 
            {
                client.BaseAddress = new Uri(Configuration.GetValue<string>("WeatherApiBaseUrl"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            services.AddSingleton<IReadOnlyDictionary<string,string>>(new Dictionary<string,string>()
            {
                {CircuitBreaker.Services.WeatherService.Models.WeatherApis.WeatherApi, Configuration.GetValue<string>("WeatherApiKey")},
                {CircuitBreaker.Services.WeatherService.Models.WeatherApis.OpenWeatherApi, Configuration.GetValue<string>("OpenWeatherApiKey")}
            });

            services.AddSingleton<IWeatherService>(serviceBuilder => 
                new WeatherService(serviceBuilder.GetService<IHttpClientFactory>()
                                   .CreateClient(CircuitBreaker.Services.WeatherService.Models.WeatherApis.OpenWeatherApi), 
                serviceBuilder.GetService<IHttpClientFactory>()
                                   .CreateClient(CircuitBreaker.Services.WeatherService.Models.WeatherApis.WeatherApi), 
                serviceBuilder.GetService<IReadOnlyDictionary<string,string>>()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
