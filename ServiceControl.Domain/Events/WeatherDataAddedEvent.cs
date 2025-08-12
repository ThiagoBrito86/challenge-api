using ServiceControl.Domain.Enums;

namespace ServiceControl.Domain.Events;

public record WeatherDataAddedEvent(
    Guid WorkRecordId,
    decimal Temperature,
    WeatherCondition Condition) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}