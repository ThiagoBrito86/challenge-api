using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceControl.Domain.Intefaces.MessageBrokers;
using ServiceControl.Infrastructure.Services.Resilience;
using System.Text;
using System.Text.Json;

namespace ServiceControl.Infrastructure.MessageBrokers.Http;

public class HttpMessageBroker<TMessage> : IMessageBroker<TMessage>
{
    private readonly HttpClient _httpClient;
    private readonly IRetryPolicy _retryPolicy;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HttpMessageBroker<TMessage>> _logger;

    public HttpMessageBroker(
        HttpClient httpClient,
        IRetryPolicy retryPolicy,
        IConfiguration configuration,
        ILogger<HttpMessageBroker<TMessage>> logger)
    {
        _httpClient = httpClient;
        _retryPolicy = retryPolicy;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string destination, TMessage message, CancellationToken cancellationToken = default)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            var serviceBUrl = _configuration["ServiceB:Url"] ?? "http://localhost:3001";
            var url = $"{serviceBUrl}/receber-registro";

            var json = JsonSerializer.Serialize(new
            {
                RegistroId = Guid.NewGuid(),
                DadosCompletos = message,
                Origem = "ServiceControl"
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending message to {Url}", url);

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Message sent successfully to {Destination}", destination);
        }, cancellationToken);
    }

    public async Task SendBatchAsync(string destination, IEnumerable<TMessage> messages, CancellationToken cancellationToken = default)
    {
        var tasks = messages.Select(message => SendAsync(destination, message, cancellationToken));
        await Task.WhenAll(tasks);
    }
}