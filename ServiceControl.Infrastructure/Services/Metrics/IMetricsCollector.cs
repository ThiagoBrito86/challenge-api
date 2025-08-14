
namespace ServiceControl.Infrastructure.Services.Metrics;

public interface IMetricsCollector
{
    void RecordRequest(TimeSpan processingTime);
    void RecordError();
    void RecordRetryAttempt();
    MetricsSnapshot GetSnapshot();
}

public record MetricsSnapshot(
    long TotalRequests,
    long TotalErrors,
    double ErrorRate,
    long RetryAttempts,
    double AverageResponseTime,
    double P95ResponseTime,
    double P99ResponseTime,
    DateTime LastReset);