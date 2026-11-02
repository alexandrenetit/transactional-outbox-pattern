using FluentValidation;

namespace SalesSample.Application.Sales.Queries.GetSale;

public sealed class GetSaleValidator : AbstractValidator<GetSaleQuery>
{
    public GetSaleValidator() => RuleFor(x => x.SaleId).NotEmpty();
}