using ServiceControl.Infrastructure.Services.Metrics;
using System.Diagnostics;

namespace ServiceControl.Middleware;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsCollector _metricsCollector;

    public MetricsMiddleware(RequestDelegate next, IMetricsCollector metricsCollector)
    {
        _next = next;
        _metricsCollector = metricsCollector;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _metricsCollector.RecordRequest(stopwatch.Elapsed);

            if (context.Response.StatusCode >= 400)
            {
                _metricsCollector.RecordError();
            }
        }
    }
}