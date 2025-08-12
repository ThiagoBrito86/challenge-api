using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Sockets;

namespace ServiceControl.Infrastructure.Services.Resilience;

public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly IAsyncPolicy _policy;
    private readonly ILogger<ExponentialBackoffRetryPolicy> _logger;

    public ExponentialBackoffRetryPolicy(ILogger<ExponentialBackoffRetryPolicy> logger)
    {
        _logger = logger;
        _policy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<SocketException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Tentativas {RetryCount} após {Delay}ms. Exception: {Exception}",
                        retryCount, timespan.TotalMilliseconds, outcome.InnerException?.Message);
                });
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return await _policy.ExecuteAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await operation();
        });
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await _policy.ExecuteAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            await operation();
        });
    }
}