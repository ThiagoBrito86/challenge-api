using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly.Retry;
using ServiceControl.Domain.Intefaces.Services;
using ServiceControl.Domain.ValueObjects;

namespace ServiceControl.Infrastructure.Services;

public class OpenWeatherMapService : IWeatherService
{
    private readonly HttpClient _httpClient;
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
                var url = $"weather?q={city}&appid={_apiKey}&units=metric&lang=pt_br";
                var response = await _httpClient.GetAsync(url, cancellationToken);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var weatherResponse = JsonSerializer.Deserialize<OpenWeatherMapResponse>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (weatherResponse == null)
                    throw new WeatherServiceException("weather service com resposta inválida");

                return new WeatherData(
                    (decimal)weatherResponse.Main.Temp,
                    weatherResponse.Weather.First().Description,
                    weatherResponse.Main.Humidity,
                    weatherResponse.Main.Pressure);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro HTTP ao obter dados de weather service para cidade : {City}", city);
                throw new WeatherServiceException($"Weather service error: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON erro de conversão de dados para weather");
                throw new WeatherServiceException("Formato inválido", ex);
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