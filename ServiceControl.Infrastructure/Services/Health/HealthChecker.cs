using Microsoft.Extensions.Logging;
using ServiceControl.Domain.Intefaces.Repositories;
using ServiceControl.Domain.Intefaces.Services;
using System.Diagnostics;


namespace ServiceControl.Infrastructure.Services.Health;

public class HealthChecker : IHealthChecker
{
    private readonly IWeatherService _weatherService;
    private readonly IWorkRecordRepository _repository;
    private readonly ILogger<HealthChecker> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public HealthChecker(
        IWeatherService weatherService,
        IWorkRecordRepository repository,
        ILogger<HealthChecker> logger)
    {
        _weatherService = weatherService;
        _repository = repository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var services = new Dictionary<string, ServiceHealth>();
        var overallStatus = "Healthy";

        // Check Weather Service
        services["WeatherService"] = await CheckWeatherServiceAsync(cancellationToken);

        // Check Database
        services["Database"] = await CheckDatabaseAsync(cancellationToken);

        // Determine overall status
        if (services.Values.Any(s => s.Status == "Unhealthy"))
            overallStatus = "Unhealthy";
        else if (services.Values.Any(s => s.Status == "Degraded"))
            overallStatus = "Degraded";

        return new HealthCheckResult(
            overallStatus,
            services,
            DateTime.UtcNow,
            DateTime.UtcNow - _startTime);
    }

    private async Task<ServiceHealth> CheckWeatherServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var isHealthy = await _weatherService.IsServiceHealthyAsync(cancellationToken);
            stopwatch.Stop();

            return new ServiceHealth(
                isHealthy ? "Healthy" : "Unhealthy",
                $"{stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao tentar consumir serviço de clima");
            return new ServiceHealth("Unhealthy", ErrorMessage: ex.Message);
        }
    }

    private async Task<ServiceHealth> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            await _repository.GetAllAsync(cancellationToken);
            stopwatch.Stop();

            return new ServiceHealth("Healthy", $"{stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao tentar acessar o banco de dados");
            return new ServiceHealth("Unhealthy", ErrorMessage: ex.Message);
        }
    }
}