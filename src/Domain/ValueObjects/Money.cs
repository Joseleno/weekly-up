using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Exceptions;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    private const string DefaultCurrency = "BRL";
    private const int DecimalPlaces = 2;

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = DefaultCurrency;

    // Construtor sem parâmetros exigido pelo EF Core para deserialização ToJson
#pragma warning disable CS8618
    private Money() { }
#pragma warning restore CS8618

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
