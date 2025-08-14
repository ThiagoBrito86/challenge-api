
namespace ServiceControl.Infrastructure.Services.Health;

public interface IHealthChecker
{
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public record HealthCheckResult(
    string Status,
    Dictionary<string, ServiceHealth> Services,
    DateTime Timestamp,
    TimeSpan Uptime);

public record ServiceHealth(
    string Status,
    string? ResponseTime = null,
    string? ErrorMessage = null);