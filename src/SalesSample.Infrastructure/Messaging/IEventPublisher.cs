namespace SalesSample.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken = default);
}