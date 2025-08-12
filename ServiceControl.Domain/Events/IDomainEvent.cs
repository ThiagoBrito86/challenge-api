using MediatR;

namespace ServiceControl.Domain.Events;

public interface IDomainEvent : INotification
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}