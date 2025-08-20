using Microsoft.AspNetCore.Mvc;
using ServiceControl.Application.DTOs;
using ServiceControl.Application.UseCases.ProcessWorkRecord;
using ServiceControl.Domain.Intefaces.Repositories;
using ServiceControl.Models.Requests;
using ServiceControl.Models.Responses;

namespace ServiceControl.Controllers;


[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkRecordController : ControllerBase
{
    private readonly IProcessWorkRecordUseCase _processWorkRecordUseCase;
    private readonly IBatchProcessWorkRecordUseCase _batchProcessUseCase;
    private readonly IWorkRecordRepository _repository;
    private readonly ILogger<WorkRecordController> _logger;

    public WorkRecordController(
        IProcessWorkRecordUseCase processWorkRecordUseCase,
        IBatchProcessWorkRecordUseCase batchProcessUseCase,
        IWorkRecordRepository repository,
        ILogger<WorkRecordController> logger)
    {
        _processWorkRecordUseCase = processWorkRecordUseCase;
        _batchProcessUseCase = batchProcessUseCase;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Registra um novo serviço executado na obra
    /// </summary>
    /// <param name="request">Dados do serviço executado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Registro processado com informações climáticas</returns>
    [HttpPost("registrar-servico")]
    [ProducesResponseType(typeof(ApiResponse<WorkRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WorkRecordResponse>>> ProcessWorkRecord(
        [FromBody] WorkRecordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = new WorkRecordRequestDto(
                request.ServicoExecutado,
                request.Data,
                request.Responsavel,
                request.Cidade);

            var result = await _processWorkRecordUseCase.ExecuteAsync(dto, cancellationToken);

            var response = new WorkRecordResponse(
                result.Id,
                result.ServicoExecutado,
                result.Data,
                result.Responsavel,
                result.Cidade,
                result.TemperaturaAtual,
                result.CondicaoMeteorologica,
                result.CondicaoClimatica,
                result.PodeExecutarObra,
                result.HorarioProcessamento);

            return Ok(new ApiResponse<WorkRecordResponse>(
                true,
                response,
                "Registro processado com sucesso"));
        }
        catch (Application.Exceptions.ValidationException ex)
        {
            _logger.LogWarning("Validation error: {Errors}", string.Join(", ", ex.Failures.Select(f => f.ErrorMessage)));
            return BadRequest(new ApiResponse<object>(
                false,
                Errors: ex.Failures.Select(f => f.ErrorMessage)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing work record");
            return StatusCode(500, new ApiResponse<object>(
                false,
                Message: "Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Processa múltiplos registros em lote
    /// </summary>
    /// <param name="request">Lote de registros para processar</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado do processamento em lote</returns>
    [HttpPost("registrar-lote")]
    [ProducesResponseType(typeof(ApiResponse<BatchProcessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BatchProcessResponse>>> ProcessBatch(
        [FromBody] BatchWorkRecordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dtos = request.Records.Select(r => new WorkRecordRequestDto(
                r.ServicoExecutado,
                r.Data,
                r.Responsavel,
                r.Cidade));

            var batchRequest = new BatchProcessRequestDto(dtos, request.BatchSize);
            var result = await _batchProcessUseCase.ExecuteAsync(batchRequest, cancellationToken);

            var response = new BatchProcessResponse(
                result.ProcessedCount,
                result.ErrorCount,
                result.Errors);

            return Ok(new ApiResponse<BatchProcessResponse>(
                true,
                response,
                $"Lote processado: {result.ProcessedCount} sucessos, {result.ErrorCount} erros"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch");
            return StatusCode(500, new ApiResponse<object>(
                false,
                Message: "Erro no processamento em lote"));
        }
    }

    /// <summary>
    /// Obtém todos os registros processados
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de registros</returns>
    [HttpGet("registros")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<WorkRecordResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<WorkRecordResponse>>>> GetRecords(
        CancellationToken cancellationToken)
    {
        try
        {
            var records = await _repository.GetAllAsync(cancellationToken);

            var responses = records.Select(r => new WorkRecordResponse(
                r.Id,
                r.ExecutedService,
                r.Date,
                r.Responsible,
                r.City,
                r.WeatherData?.Temperature.Value,
                r.WeatherData?.Description,
                r.GetWeatherConditionDescription(),
                r.CanExecuteWork(),
                r.ProcessingTime));

            return Ok(new ApiResponse<IEnumerable<WorkRecordResponse>>(
                true,
                responses,
                $"{records.Count()} registros encontrados"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting records");
            return StatusCode(500, new ApiResponse<object>(
                false,
                Message: "Erro ao obter registros"));
        }
    }

    /// <summary>
    /// Obtém um registro específico por ID
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Registro específico</returns>
    [HttpGet("registros/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkRecordResponse>>> GetRecord(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await _repository.GetByIdAsync(id, cancellationToken);

            if (record == null)
            {
                return NotFound(new ApiResponse<object>(
                    false,
                    Message: "Registro não encontrado"));
            }

            var response = new WorkRecordResponse(
                record.Id,
                record.ExecutedService,
                record.Date,
                record.Responsible,
                record.City,
                record.WeatherData?.Temperature.Value,
                record.WeatherData?.Description,
                record.GetWeatherConditionDescription(),
                record.CanExecuteWork(),
                record.ProcessingTime);

            return Ok(new ApiResponse<WorkRecordResponse>(
                true,
                response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting record {Id}", id);
            return StatusCode(500, new ApiResponse<object>(
                false,
                Message: "Erro ao obter registro"));
        }
    }
}