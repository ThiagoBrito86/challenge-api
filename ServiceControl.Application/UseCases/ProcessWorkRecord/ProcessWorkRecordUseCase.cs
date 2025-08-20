using FluentValidation;
using ServiceControl.Application.DTOs;
using MediatR;
using ServiceControl.Domain.Intefaces.Services;
using ServiceControl.Domain.Intefaces.Repositories;
using ServiceControl.Domain.Intefaces.MessageBrokers;
using Microsoft.Extensions.Logging;
using ServiceControl.Domain.Entities;

namespace ServiceControl.Application.UseCases.ProcessWorkRecord;

public class ProcessWorkRecordUseCase : IProcessWorkRecordUseCase
{
    private readonly IWorkRecordRepository _repository;
    private readonly IWeatherService _weatherService;
    private readonly IMessageBroker<WorkRecordResponseDto> _messageBroker;
    private readonly IValidator<WorkRecordRequestDto> _validator;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessWorkRecordUseCase> _logger;

    public ProcessWorkRecordUseCase(
        IWorkRecordRepository repository,
        IWeatherService weatherService,
        IMessageBroker<WorkRecordResponseDto> messageBroker,
        IValidator<WorkRecordRequestDto> validator,
        IMediator mediator,
        ILogger<ProcessWorkRecordUseCase> logger)
    {
        _repository = repository;
        _weatherService = weatherService;
        _messageBroker = messageBroker;
        _validator = validator;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<WorkRecordResponseDto> ExecuteAsync(WorkRecordRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processando registro para serviço : {Service} na cidade: {City}", request.ServicoExecutado, request.Cidade);

        // Validar entrada
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        //entidade
        var workRecord = new WorkRecord(
            request.ServicoExecutado,
            request.Data,
            request.Responsavel,
            request.Cidade);

        try
        {
            // Obter dados climáticos
            var weatherData = await _weatherService.GetWeatherDataAsync(request.Cidade, request.Data, cancellationToken);
            workRecord.AddWeatherInformation(weatherData);

            // Persistir
            var savedRecord = await _repository.AddAsync(workRecord, cancellationToken);

            // Publicar eventos de domínio
            await PublishDomainEventsAsync(savedRecord, cancellationToken);

            // Criar resposta
            var response = CreateResponseDto(savedRecord);

            // Enviar para ServiceB
            //await _messageBroker.SendAsync("service-b-queue", response, cancellationToken);

            _logger.LogInformation("Registro processado: {Id}", savedRecord.Id);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar registro para o serviço : {Service}", request.ServicoExecutado);
            throw;
        }
    }

    private async Task PublishDomainEventsAsync(WorkRecord workRecord, CancellationToken cancellationToken)
    {
        var domainEvents = workRecord.DomainEvents.ToList();
        workRecord.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }

    private static WorkRecordResponseDto CreateResponseDto(WorkRecord workRecord)
    {
        return new WorkRecordResponseDto(
            workRecord.Id,
            workRecord.ExecutedService,
            workRecord.Date,
            workRecord.Responsible,
            workRecord.City,
            workRecord.WeatherData?.Temperature.Value,
            workRecord.WeatherData?.Description,
            workRecord.GetWeatherConditionDescription(),
            workRecord.CanExecuteWork(),
            workRecord.ProcessingTime);
    }
}