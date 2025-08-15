using ServiceControl.Domain.ValueObjects;

namespace ServiceControl.Domain.Intefaces.Services;

public interface IWeatherService
{
    Task<WeatherData> GetWeatherDataAsync(string city, DateTime date, CancellationToken cancellationToken = default);
    Task<bool> IsServiceHealthyAsync(CancellationToken cancellationToken = default);
}