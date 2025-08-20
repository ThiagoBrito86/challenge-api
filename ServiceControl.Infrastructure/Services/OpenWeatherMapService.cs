using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceControl.Application.Exceptions;
using ServiceControl.Domain.Intefaces.Services;
using ServiceControl.Domain.ValueObjects;
using ServiceControl.Infrastructure.Services.Resilience;

namespace ServiceControl.Infrastructure.Services;

public class OpenWeatherMapService : IWeatherService
{
    private readonly HttpClient _httpClient;
    //private readonly IAsyncPolicy<WeatherData> _retryPolicy; 
    private readonly IRetryPolicy _retryPolicy;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenWeatherMapService> _logger;
    private readonly string _apiKey;

    public OpenWeatherMapService(
        HttpClient httpClient,
         IRetryPolicy retryPolicy,
        IConfiguration configuration,
        ILogger<OpenWeatherMapService> logger)
    {
        _httpClient = httpClient;
        _retryPolicy = retryPolicy;
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["WeatherApi:ApiKey"] ?? throw new InvalidOperationException("Weather API sem dados de configuração");
    }

    public async Task<WeatherData> GetWeatherDataAsync(string city, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                _logger.LogInformation("Fetching weather data for city: {City}", city);

                // Construir URL com parâmetros corretos
                var queryParams = new List<string>
                {
                    $"q={Uri.EscapeDataString(city)}",
                    $"APPID={_apiKey}",
                    $"units=metric",
                    $"lang=pt_br"
                };

                var url = $"weather?{string.Join("&", queryParams)}";

                _logger.LogDebug("Making request to: {Url}", url.Replace(_apiKey, "***"));

                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Weather API error. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, errorContent);

                    throw new WeatherServiceException($"Weather API returned {response.StatusCode}: {errorContent}");
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("Weather API response: {Content}", content);

                var weatherResponse = JsonSerializer.Deserialize<OpenWeatherMapResponse>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (weatherResponse == null)
                    throw new WeatherServiceException("Invalid response from weather service");

                var weatherData = new WeatherData(
                    (decimal)weatherResponse.Main.Temp,
                    weatherResponse.Weather.First().Description,
                    weatherResponse.Main.Humidity,
                    weatherResponse.Main.Pressure);

                _logger.LogInformation("Weather data retrieved successfully for {City}. Temperature: {Temperature}°C",
                    city, weatherData.Temperature.Value);

                return weatherData;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error getting weather data for city: {City}", city);
                throw new WeatherServiceException($"Weather service HTTP error: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout getting weather data for city: {City}", city);
                throw new WeatherServiceException($"Weather service timeout: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for weather data from city: {City}", city);
                throw new WeatherServiceException("Invalid weather data format", ex);
            }
        }, cancellationToken);
    }

    public async Task<bool> IsServiceHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await GetWeatherDataAsync("São Paulo", DateTime.UtcNow, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private record OpenWeatherMapResponse(
        MainData Main,
        WeatherInfo[] Weather);

    private record MainData(
        double Temp,
        int Humidity,
        int Pressure);

    private record WeatherInfo(
        string Description);
}