using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Exceptions;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    private const string DefaultCurrency = "BRL";
    private const int DecimalPlaces = 2;

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = Math.Round(amount, DecimalPlaces);
        Currency = currency;
    }

    public static Money BRL(decimal amount) => new(amount, DefaultCurrency);

    public static readonly Money Zero = new(0m, DefaultCurrency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException("Money.CurrencyMismatch", $"Nao e possivel somar moedas diferentes: {Currency} e {other.Currency}.");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException("Money.CurrencyMismatch", $"Nao e possivel subtrair moedas diferentes: {Currency} e {other.Currency}.");
        }

        return new Money(Amount - other.Amount, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Currency} {Amount:N2}";
}
