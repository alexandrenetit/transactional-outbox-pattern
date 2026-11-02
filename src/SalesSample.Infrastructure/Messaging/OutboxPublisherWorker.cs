using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SalesSample.Infrastructure.Persistence;

namespace SalesSample.Infrastructure.Messaging;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    public OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // loop infinito até o app parar (stoppingToken é cancelado no shutdown)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages.");
            }
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        // cria um escopo novo (DbContext é Scoped, worker é Singleton)
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        
        // pega até 100 mensagens pendentes, na ordem em que ocorreram
        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Take(100)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(
                    message.Type,          // tópico = nome do evento ("SaleCreatedEvent")
                    message.Id.ToString(),  // key = EventId
                    message.Payload,            // JSON
                    ct);
                
                message.MarkProcessed(DateTime.UtcNow);
                await dbContext.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}.", message.Id);
                // ProcessedOn continua NULL → será reprocessado no próximo ciclo (at-least-once)
            }
        }
    }
}