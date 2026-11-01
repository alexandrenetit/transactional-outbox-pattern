namespace SalesSample.Domain.ValueObjects;

public sealed record Money(decimal Amount, string Currency)
{
private Money() :this(0, "USD") { } // EF Core materializa por aqui

    public static Money Zero(string currency) => new(0, currency);

    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money money, int multiplier) =>
        new(money.Amount * multiplier, money.Currency);

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Money currencies must match.");
    }
}