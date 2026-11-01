using SalesSample.Domain.Common;

namespace SalesSample.Domain.Events;

public sealed class SaleCreatedEvent : DomainEvent
{
    public SaleCreatedEvent(Guid saleId, string saleNumber, Guid customerId, Guid branchId)
    {
        SaleId = saleId;
        SaleNumber = saleNumber;
        CustomerId = customerId;
        BranchId = branchId;
    }

    public Guid SaleId { get; }
    public string SaleNumber { get; }
    public Guid CustomerId { get; }
    public Guid BranchId { get; }
}