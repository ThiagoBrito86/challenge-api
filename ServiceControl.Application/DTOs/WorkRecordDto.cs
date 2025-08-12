namespace ServiceControl.Application.DTOs;

public record WorkRecordRequestDto(
    string ServicoExecutado,
    DateTime Data,
    string Responsavel,
    string Cidade);

public record WorkRecordResponseDto(
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

public record BatchProcessRequestDto(
    IEnumerable<WorkRecordRequestDto> Records,
    int BatchSize = 100);

public record BatchProcessResponseDto(
    int ProcessedCount,
    int ErrorCount,
    IEnumerable<string> Errors);