using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PluralsightAuth.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var currentAccessToken = await HttpContext.GetTokenAsync("access_token");

            var httpClient = _httpClientFactory.CreateClient();

            var responseFromTokenEndpoint = await httpClient.RequestTokenAsync(
                new TokenRequest
                {
                    Address = "https://login.microsoftonline.com/002da039-bedb-437a-aab3-c65817a15f85/oauth2/v2.0/token",
                    GrantType = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ClientId = "2c6fb12c-9701-496c-9460-a8df8ae1e658",
                    ClientSecret = "-zD7Q~WDsbzjEAkHLUhYe.cMJeK15Q5iycHRf",
                    Parameters =
                    {
                        {"assertion",currentAccessToken },
                        {"scope","api://743b4485-08fb-4ddc-9685-8864b9d4cea7/FullAcessApi2" },
                        {"requested_token_use","on_behalf_of" }
                    }
                });

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://localhost:44356/weatherforecast");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", responseFromTokenEndpoint.AccessToken);

            var response = await httpClient.SendAsync(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                //issue
                throw new Exception(response.ReasonPhrase);
            }

            return await JsonSerializer.DeserializeAsync<IEnumerable<WeatherForecast>>(
                await response.Content.ReadAsStreamAsync());
        }
    }
}
