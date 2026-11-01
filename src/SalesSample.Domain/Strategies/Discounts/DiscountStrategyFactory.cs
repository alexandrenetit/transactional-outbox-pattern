using SalesSample.Domain.Exceptions;
using SalesSample.Domain.Strategies.Discounts.Enums;

namespace SalesSample.Domain.Strategies.Discounts;

public static class DiscountStrategyFactory
{
    public static IDiscountStrategy CreateStrategy(
        DiscountStrategyType type = DiscountStrategyType.Standard) =>
        type switch
        {
            DiscountStrategyType.Standard => new StandardDiscountStrategy(),
            _ => throw new DomainException($"Unsupported discount strategy type: {type}")
        };
}
