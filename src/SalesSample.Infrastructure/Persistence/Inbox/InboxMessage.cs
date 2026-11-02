namespace SalesSample.Infrastructure.Persistence.Inbox;

public sealed class InboxMessage
{
    private InboxMessage() { }  // EF

    public InboxMessage(Guid eventId, DateTime processedOn)
    {
        EventId = eventId;
        ProcessedOn = processedOn;
    }

    public Guid EventId { get; private set; }     // chave primária = id do evento
    public DateTime ProcessedOn { get; private set; }
}