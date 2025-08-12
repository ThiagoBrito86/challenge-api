namespace ServiceControl.Domain.Intefaces.MessageBrokers;


public interface IMessageBroker<in TMessage>
{
    Task SendAsync(string destination, TMessage message, CancellationToken cancellationToken = default);
    Task SendBatchAsync(string destination, IEnumerable<TMessage> messages, CancellationToken cancellationToken = default);
}