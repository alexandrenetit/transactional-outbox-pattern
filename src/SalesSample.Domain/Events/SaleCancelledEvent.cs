using SalesSample.Domain.Common;

namespace SalesSample.Domain.Events;

public sealed class SaleCancelledEvent : DomainEvent
{
    public SaleCancelledEvent(Guid saleId, string saleNumber)
    {
        SaleId = saleId;
        SaleNumber = saleNumber;
    }

    public Guid SaleId { get; }
    public string SaleNumber { get; }
}