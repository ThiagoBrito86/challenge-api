
using System.Collections.Concurrent;

namespace ServiceControl.Infrastructure.Services.Metrics;

public class MetricsCollector : IMetricsCollector
{
    private long _totalRequests;
    private long _totalErrors;
    private long _retryAttempts;
    private readonly ConcurrentQueue<double> _responseTimes = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    private const int MaxResponseTimes = 1000;

    public void RecordRequest(TimeSpan processingTime)
    {
        Interlocked.Increment(ref _totalRequests);

        _responseTimes.Enqueue(processingTime.TotalMilliseconds);

        // Manter apenas os últimos 1000 tempos de resposta
        while (_responseTimes.Count > MaxResponseTimes)
        {
            _responseTimes.TryDequeue(out _);
        }
    }

    public void RecordError()
    {
        Interlocked.Increment(ref _totalErrors);
    }

    public void RecordRetryAttempt()
    {
        Interlocked.Increment(ref _retryAttempts);
    }

    public MetricsSnapshot GetSnapshot()
    {
        var totalRequests = _totalRequests;
        var totalErrors = _totalErrors;
        var retryAttempts = _retryAttempts;
        var errorRate = totalRequests > 0 ? (double)totalErrors / totalRequests * 100 : 0;

        var responseTimes = _responseTimes.ToArray();
        var averageResponseTime = responseTimes.Length > 0 ? responseTimes.Average() : 0;
        var p95ResponseTime = CalculatePercentile(responseTimes, 0.95);
        var p99ResponseTime = CalculatePercentile(responseTimes, 0.99);

        return new MetricsSnapshot(
            totalRequests,
            totalErrors,
            Math.Round(errorRate, 2),
            retryAttempts,
            Math.Round(averageResponseTime, 2),
            Math.Round(p95ResponseTime, 2),
            Math.Round(p99ResponseTime, 2),
            _startTime);
    }

    private static double CalculatePercentile(double[] values, double percentile)
    {
        if (values.Length == 0) return 0;

        var sortedValues = values.OrderBy(x => x).ToArray();
        var index = (int)Math.Ceiling(percentile * sortedValues.Length) - 1;
        return sortedValues[Math.Max(0, index)];
    }
}