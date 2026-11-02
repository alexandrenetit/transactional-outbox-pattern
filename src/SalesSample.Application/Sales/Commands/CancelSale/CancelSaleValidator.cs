using FluentValidation;

namespace SalesSample.Application.Sales.Commands.CancelSale;

public sealed class CancelSaleValidator : AbstractValidator<CancelSaleCommand>
{
    public CancelSaleValidator() => RuleFor(x => x.SaleId).NotEmpty();
}