using MediatR;
using SalesSample.Domain.Exceptions;
using SalesSample.Domain.Repositories;
using SalesSample.Domain.Services;

namespace SalesSample.Application.Sales.Commands.CreateSale;

public sealed class CreateSaleHandler : IRequestHandler<CreateSaleCommand, CreateSaleResult>
{
    private readonly ISaleService _saleService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateSaleHandler(
        ISaleService saleService,
        ICustomerRepository customerRepository,
        IBranchRepository branchRepository,
        IProductRepository productRepository,
        ISaleRepository saleRepository,
        IUnitOfWork unitOfWork)
    {
        _saleService = saleService;
        _customerRepository = customerRepository;
        _branchRepository = branchRepository;
        _productRepository = productRepository;
        _saleRepository = saleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateSaleResult> Handle(
        CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken)
                       ?? throw new DomainException($"Customer {command.CustomerId} not fount.");
        
        var branch = await _branchRepository.GetByIdAsync(command.BranchId, cancellationToken)
                 ?? throw new DomainException($"Branch {command.BranchId} not fount.");

        var productsIds = command.Items.Select((i => i.ProductId)).ToList();
        var products = await _productRepository.GetByIdsAsync(productsIds, cancellationToken);

        var items = command.Items.Select(i =>
            {
                var product = products.FirstOrDefault(p => p.Id == i.ProductId) 
                    ?? throw new DomainException($"Product {i.ProductId} not fount.");
                return (product, i.Quantity);
            });

        var sale = _saleService.CreateSale(customer, branch, command.SaleNumber, items);

        await _saleRepository.AddAsync(sale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateSaleResult(sale.Id, sale.SaleNumber, sale.TotalAmount.Amount);
    }
}

