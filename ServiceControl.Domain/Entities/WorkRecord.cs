using ServiceControl.Domain.Events;
using ServiceControl.Domain.ValueObjects;

namespace ServiceControl.Domain.Entities;

public class WorkRecord
{
    public Guid Id { get; private set; }
    public string ExecutedService { get; private set; }
    public DateTime Date { get; private set; }
    public string Responsible { get; private set; }
    public string City { get; private set; }
    public WeatherData? WeatherData { get; private set; }
    public DateTime ProcessingTime { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected WorkRecord() { } // EF Core

    public WorkRecord(
        string executedService,
        DateTime date,
        string responsible,
        string city)
    {
        Id = Guid.NewGuid();
        ExecutedService = executedService ?? throw new ArgumentNullException(nameof(executedService));
        Date = date;
        Responsible = responsible ?? throw new ArgumentNullException(nameof(responsible));
        City = city ?? throw new ArgumentNullException(nameof(city));
        ProcessingTime = DateTime.UtcNow;

        AddDomainEvent(new WorkRecordCreatedEvent(Id, executedService, city));
    }

    public void AddWeatherInformation(WeatherData weatherData)
    {
        WeatherData = weatherData ?? throw new ArgumentNullException(nameof(weatherData));

        AddDomainEvent(new WeatherDataAddedEvent(Id, weatherData.Temperature.Value, weatherData.Temperature.Condition));
    }

    public bool CanExecuteWork() => WeatherData?.Temperature.CanExecuteWork() ?? false;

    public string GetWeatherConditionDescription() => WeatherData?.Temperature.GetConditionDescription() ?? "não informado";

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}