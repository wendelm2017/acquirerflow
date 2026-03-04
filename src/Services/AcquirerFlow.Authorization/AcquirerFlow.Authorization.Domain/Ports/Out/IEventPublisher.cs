namespace AcquirerFlow.Authorization.Domain.Ports.Out;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string topic) where T : class;
}
