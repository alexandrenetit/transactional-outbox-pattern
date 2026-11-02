using MediatR;
using SalesSample.Domain.Exceptions;
using SalesSample.Domain.Repositories;
using SalesSample.Domain.Services;

namespace SalesSample.Application.Sales.Commands.CancelSale;

public sealed class CancelSaleHandler : IRequestHandler<CancelSaleCommand>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ISaleService _saleService;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSaleHandler(
        ISaleRepository saleRepository,
        ISaleService saleService,
        IUnitOfWork unitOfWork)
    {
        _saleRepository = saleRepository;
        _saleService = saleService;
        _unitOfWork = unitOfWork;
    }
    
    public async Task Handle(CancelSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.SaleId, cancellationToken)
            ?? throw new DomainException($"Sale {command.SaleId} not found.");
        
        _saleService.CancelSale(sale); // Dispara SaleCancelledEvent

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}