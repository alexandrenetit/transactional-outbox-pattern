using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SalesSample.Infrastructure.Persistence;
using SalesSample.Infrastructure.Persistence.Inbox;

namespace SalesSample.Infrastructure.Messaging;

public sealed class KafkaConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumer> _logger;
    private static readonly string[] Topics = { "SaleCreatedEvent", "SaleCancelledEvent" };

    public KafkaConsumer(
        IServiceScopeFactory scopeFactory,
        IConsumer<string, string> consumer,
        ILogger<KafkaConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _consumer = consumer;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken ct)
    {
        _consumer.Subscribe(Topics);   // assina os tópicos (um por tipo de evento)

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(ct);   // bloqueia até chegar mensagem
                Process(result.Message, result.Topic);
                _consumer.Commit(result);             // commit do offset APÓS processar
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming message");
            }
        }

        _consumer.Close();
    }

    private void Process(Message<string, string> message, string topic)
    {
        var eventId = Guid.Parse(message.Key);   // a key que o worker mandou = EventId

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        // 1. Idempotência: já processei esse EventId?
        if (dbContext.InboxMessages.Any(m => m.EventId == eventId))
        {
            _logger.LogInformation("Duplicate event {EventId} ignored.", eventId);
            return;   // duplicado → ignora
        }

        // 2. Processa (aqui só loga; num sistema real, faria o side-effect real)
        _logger.LogInformation(
            "Processed {Topic} (EventId {EventId}): {Payload}",
            topic, eventId, message.Value);

        // 3. Marca como processado (dedupe durável)
        dbContext.InboxMessages.Add(new InboxMessage(eventId, DateTime.UtcNow));
        dbContext.SaveChanges();
    }
}