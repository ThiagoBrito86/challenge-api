namespace ServiceControl.Domain.Events;

public record WorkRecordCreatedEvent(
    Guid WorkRecordId,
    string ExecutedService,
    string City) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}