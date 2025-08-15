using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceControl.Infrastructure.Services.Health;
using ServiceControl.Infrastructure.Services.Metrics;
using ServiceControl.Models.Responses;

namespace ServiceControl.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthChecker _healthChecker;
    private readonly IMetricsCollector _metricsCollector;

    public HealthController(
        IHealthChecker healthChecker,
        IMetricsCollector metricsCollector)
    {
        _healthChecker = healthChecker;
        _metricsCollector = metricsCollector;
    }

    /// <summary>
    /// Health check básico
    /// </summary>
    [HttpGet]
    public ActionResult<object> GetHealth()
    {
        return Ok(new
        {
            Status = "OK",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        });
    }

    /// <summary>
    /// Health check detalhado de todas as dependências
    /// </summary>
    [HttpGet("detailed")]
    public async Task<ActionResult<HealthResponse>> GetDetailedHealth(CancellationToken cancellationToken)
    {
        var healthResult = await _healthChecker.CheckHealthAsync(cancellationToken);

        var response = new HealthResponse(
            healthResult.Status,
            healthResult.Services.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)new
                {
                    kvp.Value.Status,
                    kvp.Value.ResponseTime,
                    kvp.Value.ErrorMessage
                }),
            healthResult.Timestamp,
            healthResult.Uptime.ToString(@"dd\.hh\:mm\:ss"));

        var statusCode = healthResult.Status switch
        {
            "Healthy" => 200,
            "Degraded" => 200,
            "Unhealthy" => 503,
            _ => 503
        };

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Métricas operacionais do sistema
    /// </summary>
    [HttpGet("metrics")]
    public ActionResult<MetricsResponse> GetMetrics()
    {
        var metrics = _metricsCollector.GetSnapshot();

        var response = new MetricsResponse(
            metrics.TotalRequests,
            metrics.TotalErrors,
            metrics.ErrorRate,
            metrics.RetryAttempts,
            metrics.AverageResponseTime,
            metrics.P95ResponseTime,
            metrics.P99ResponseTime,
            metrics.LastReset);

        return Ok(response);
    }
}