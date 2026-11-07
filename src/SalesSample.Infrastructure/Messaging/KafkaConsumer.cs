using System.Diagnostics;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SalesSample.Infrastructure.Persistence;
using SalesSample.Infrastructure.Persistence.Inbox;

namespace SalesSample.Infrastructure.Messaging;

public sealed class KafkaConsumer : BackgroundService
{
    private const int MaxAttempts = 3;

    private readonly IServiceScopeFactory _scopeFactory; // cria escopo p/ DbContext
    private readonly IConsumer<string, string> _consumer; // cliente Kafka
    private readonly IEventPublisher _publisher; // publica na DLQ
    private readonly ILogger<KafkaConsumer> _logger;

    private static readonly string[] Topics = { "SaleCreatedEvent", "SaleCancelledEvent" };

    public KafkaConsumer(
        IServiceScopeFactory scopeFactory,
        IConsumer<string, string> consumer,
        IEventPublisher publisher,
        ILogger<KafkaConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _consumer = consumer;
        _publisher = publisher;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken ct)
    {
        _consumer.Subscribe(Topics); // assina os 2 tópicos

        while (!ct.IsCancellationRequested) // loop até o app parar
        {
            try
            {
                var result = _consumer.Consume(ct); // ▶️ BLOQUEIA até chegar mensagem
                ProcessWithRetry(result); // 🔁 retry + DLQ
                _consumer.Commit(result); // commit do offset APÓS processar
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

    private void ProcessWithRetry(ConsumeResult<string, string> result)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                Process(result.Message);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                // ⏳ AINDA TEM TENTATIVA → espera e tenta de novo
                _logger.LogWarning(ex, "Tentativa {Attempt}/{Max} falhou...",
                    attempt, MaxAttempts);

                Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // backoff
            }
            catch (Exception ex)
            {
                // ❌ ÚLTIMA TENTATIVA → manda pra DLQ
                _logger.LogError(ex, "Falhou após {Max} tentativas; DLQ", MaxAttempts);
                SendToDlq(result);
            }
        }
    }

    private void SendToDlq(ConsumeResult<string, string> result)
    {
        var dlqTopic = $"{result.Topic}-dlq"; // ex.: SaleCreatedEvent-dlq

        _publisher
            .PublishAsync(dlqTopic, result.Message.Key, result.Message.Value, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private void Process(Message<string, string> message) // idempotência
    {
        var eventId = Guid.Parse(message.Key);

        using var scope = _scopeFactory.CreateScope(); // DbContext novo
        var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        if (dbContext.InboxMessages.Any(m => m.EventId == eventId))
            return; // 🔁 já processou → ignora

        // ... processa (aqui só loga) ...

        dbContext.InboxMessages.Add((new InboxMessage(eventId, DateTime.UtcNow)));
        dbContext.SaveChanges();
        
        _logger.LogInformation("Processed {Topic} (EventId {EventId})", message.Key, eventId);
    }
}