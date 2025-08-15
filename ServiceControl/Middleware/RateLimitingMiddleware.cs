using System.Collections.Concurrent;
using System.Net;

namespace ServiceControl.Middleware;
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, List<DateTime>> _clientRequests = new();
    private readonly int _maxRequests = 1000; // Máximo de requests por janela
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1); // Janela de 1 minuto

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var now = DateTime.UtcNow;

        var clientRequests = _clientRequests.GetOrAdd(clientId, _ => new List<DateTime>());

        lock (clientRequests)
        {
            // Remove requisições antigas
            clientRequests.RemoveAll(time => now - time > _timeWindow);

            if (clientRequests.Count >= _maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Add("Retry-After", "60");
               //wait context.Response.WriteAsync("Rate limit exceeded. Try again later.");
                return;
            }

            clientRequests.Add(now);
            context.Response.Headers.Add("X-RateLimit-Remaining", (_maxRequests - clientRequests.Count).ToString());
        }

        await _next(context);
    }

    private static string GetClientId(HttpContext context)
    {
        // Usar IP + User-Agent como identificador único
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        return $"{ip}:{userAgent.GetHashCode()}";
    }
}