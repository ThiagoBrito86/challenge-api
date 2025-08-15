using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceControl.Application.DTOs;
using ServiceControl.Application.UseCases.ProcessWorkRecord;
using ServiceControl.Domain.Entities;
using ServiceControl.Domain.Intefaces.MessageBrokers;
using ServiceControl.Domain.Intefaces.Repositories;
using ServiceControl.Domain.Intefaces.Services;
using ServiceControl.Domain.ValueObjects;


namespace ServiceControl.UnitTests.UseCases;

public class ProcessWorkRecordUseCaseTests
{
    private readonly Mock<IWorkRecordRepository> _repositoryMock;
    private readonly Mock<IWeatherService> _weatherServiceMock;
    private readonly Mock<IMessageBroker<WorkRecordResponseDto>> _messageBrokerMock;
    private readonly Mock<IValidator<WorkRecordRequestDto>> _validatorMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ProcessWorkRecordUseCase>> _loggerMock;
    private readonly ProcessWorkRecordUseCase _useCase;

    public ProcessWorkRecordUseCaseTests()
    {
        _repositoryMock = new Mock<IWorkRecordRepository>();
        _weatherServiceMock = new Mock<IWeatherService>();
        _messageBrokerMock = new Mock<IMessageBroker<WorkRecordResponseDto>>();
        _validatorMock = new Mock<IValidator<WorkRecordRequestDto>>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ProcessWorkRecordUseCase>>();

        _useCase = new ProcessWorkRecordUseCase(
            _repositoryMock.Object,
            _weatherServiceMock.Object,
            _messageBrokerMock.Object,
            _validatorMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_ShouldReturnWorkRecordResponse()
    {
        // Arrange
        var request = new WorkRecordRequestDto(
            "Pavimentação Asfáltica",
            DateTime.Now,
            "João Silva",
            "São Paulo");

        var weatherData = new WeatherData(22m, "ensolarado", 65, 1013);
        var workRecord = new WorkRecord(request.ServicoExecutado, request.Data, request.Responsavel, request.Cidade);

        _validatorMock.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _weatherServiceMock.Setup(x => x.GetWeatherDataAsync(request.Cidade, request.Data, It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherData);

        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<WorkRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workRecord);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.ServicoExecutado, result.ServicoExecutado);
        Assert.Equal(request.Responsavel, result.Responsavel);
        Assert.Equal(request.Cidade, result.Cidade);
        Assert.True(result.PodeExecutarObra); // 22°C está em condições ótimas
        Assert.Equal("ótimas condições", result.CondicaoClimatica);

        _messageBrokerMock.Verify(x => x.SendAsync("service-b-queue", It.IsAny<WorkRecordResponseDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var request = new WorkRecordRequestDto("", DateTime.Now, "", "");

        var validationFailures = new List<ValidationFailure>
        {
            new("ServicoExecutado", "Serviço executado é obrigatório"),
            new("Responsavel", "Responsável é obrigatório"),
            new("Cidade", "Cidade é obrigatória")
        };

        _validatorMock.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(validationFailures));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => _useCase.ExecuteAsync(request));
        Assert.Contains("Serviço executado é obrigatório", exception.Message);
    }

    [Theory]
    [InlineData(5, "impraticável", false)]
    [InlineData(12, "agradável", true)]
    [InlineData(25, "ótimas condições", true)]
    [InlineData(35, "impraticável", false)]
    public async Task ExecuteAsync_DifferentTemperatures_ShouldClassifyCorrectly(
        decimal temperature, string expectedCondition, bool canExecute)
    {
        // Arrange
        var request = new WorkRecordRequestDto(
            "Teste Serviço",
            DateTime.Now,
            "João Silva",
            "São Paulo");

        var weatherData = new WeatherData(temperature, "teste", 65, 1013);
        var workRecord = new WorkRecord(request.ServicoExecutado, request.Data, request.Responsavel, request.Cidade);

        _validatorMock.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _weatherServiceMock.Setup(x => x.GetWeatherDataAsync(request.Cidade, request.Data, It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherData);

        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<WorkRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workRecord);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        Assert.Equal(expectedCondition, result.CondicaoClimatica);
        Assert.Equal(canExecute, result.PodeExecutarObra);
    }
}