namespace SalesSample.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage() { } // EF

    public OutboxMessage(Guid id, string type, string payload, DateTime occurredOn)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredOn = occurredOn;
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedOn { get; private set; }

    public void MarkProcessed(DateTime processedOn) => ProcessedOn = processedOn;
}