using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ServiceControl.Models.Requests;
using ServiceControl.Models.Responses;

namespace ServiceControl.IntegrationTests.Controllers;

public class WorkRecordControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WorkRecordControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ProcessWorkRecord_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new WorkRecordRequest(
            "Pavimentação Asfáltica",
            DateTime.Now,
            "João Silva",
            "São Paulo");

        // Act
        var response = await _client.PostAsJsonAsync("/api/workrecord/registrar-servico", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<WorkRecordResponse>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(request.ServicoExecutado, apiResponse.Data.ServicoExecutado);
    }

    [Fact]
    public async Task ProcessWorkRecord_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new WorkRecordRequest("", DateTime.Now, "", "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/workrecord/registrar-servico", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("OK", content);
    }

    [Fact]
    public async Task GetDetailedHealth_ShouldReturnHealthStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/detailed");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Status", content);
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnMetrics()
    {
        // Act
        var response = await _client.GetAsync("/api/health/metrics");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var metrics = JsonSerializer.Deserialize<MetricsResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(metrics);
        Assert.True(metrics.TotalRequests >= 0);
    }
}