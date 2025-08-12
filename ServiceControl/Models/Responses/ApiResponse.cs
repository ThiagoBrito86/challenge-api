namespace ServiceControl.Models.Responses;

public record ApiResponse<T>(
    bool Success,
    T? Data = default,
    string? Message = null,
    IEnumerable<string>? Errors = null);

public record WorkRecordResponse(
    Guid Id,
    string ServicoExecutado,
    DateTime Data,
    string Responsavel,
    string Cidade,
    decimal? TemperaturaAtual,
    string? CondicaoMeteorologica,
    string CondicaoClimatica,
    bool PodeExecutarObra,
    DateTime HorarioProcessamento);

public record BatchProcessResponse(
    int ProcessedCount,
    int ErrorCount,
    IEnumerable<string> Errors);

public record HealthResponse(
    string Status,
    Dictionary<string, object> Services,
    DateTime Timestamp,
    string Uptime);

public record MetricsResponse(
    long TotalRequests,
    long TotalErrors,
    double ErrorRate,
    long RetryAttempts,
    double AverageResponseTime,
    double P95ResponseTime,
    double P99ResponseTime,
    DateTime LastReset);