using Microsoft.Extensions.Logging;
using ServiceControl.Application.DTOs;

namespace ServiceControl.Application.UseCases.ProcessWorkRecord;

public class BatchProcessWorkRecordUseCase : IBatchProcessWorkRecordUseCase
{
    private readonly IProcessWorkRecordUseCase _processWorkRecordUseCase;
    private readonly ILogger<BatchProcessWorkRecordUseCase> _logger;

    public BatchProcessWorkRecordUseCase(
        IProcessWorkRecordUseCase processWorkRecordUseCase,
        ILogger<BatchProcessWorkRecordUseCase> logger)
    {
        _processWorkRecordUseCase = processWorkRecordUseCase;
        _logger = logger;
    }

    public async Task<BatchProcessResponseDto> ExecuteAsync(BatchProcessRequestDto request, CancellationToken cancellationToken = default)
    {
        var records = request.Records.ToList();
        var processedCount = 0;
        var errors = new List<string>();

        _logger.LogInformation("Iniciar processo de {Count} registros com tamanho: {BatchSize}",
            records.Count, request.BatchSize);

        for (int i = 0; i < records.Count; i += request.BatchSize)
        {
            var batch = records.Skip(i).Take(request.BatchSize);
            var batchNumber = (i / request.BatchSize) + 1;

            _logger.LogInformation("Processando lote {BatchNumber}", batchNumber);

            var tasks = batch.Select(async record =>
            {
                try
                {
                    await _processWorkRecordUseCase.ExecuteAsync(record, cancellationToken);
                    Interlocked.Increment(ref processedCount);
                }
                catch (Exception ex)
                {
                    var error = $"Erro ao processar registro {record.ServicoExecutado}: {ex.Message}";
                    lock (errors) { errors.Add(error); }
                    _logger.LogError(ex, "Erro no processamento do lote: {Error}", error);
                }
            });

            await Task.WhenAll(tasks);
        }

        _logger.LogInformation("Lote processado. Sucesso: {Processed}, Falha: {Errors}",
            processedCount, errors.Count);

        return new BatchProcessResponseDto(processedCount, errors.Count, errors);
    }
}