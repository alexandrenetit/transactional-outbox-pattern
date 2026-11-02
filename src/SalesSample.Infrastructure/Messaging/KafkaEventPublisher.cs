using Confluent.Kafka;

namespace SalesSample.Infrastructure.Messaging;

public class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;

    public KafkaEventPublisher(IProducer<string, string> producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync(
        string topic, string key, string payload, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = key,      // EventId (chave de partição + idempotência)
            Value = payload // JSON do evento
        };

        await _producer.ProduceAsync(topic, message, cancellationToken);
    }
}