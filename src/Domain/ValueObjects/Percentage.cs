using WeeklyUp.Domain.Common;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class Percentage : ValueObject
{
    private const int DecimalPlaces = 1;
    private const decimal HundredPercent = 100m;

    public decimal Value { get; }

    private Percentage(decimal value) => Value = Math.Round(value, DecimalPlaces);

    public static Percentage FromValue(decimal value) => new(value);

    public static Percentage CalculateChange(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            return new Percentage(current == 0m ? 0m : HundredPercent);
        }

        return new Percentage((current - previous) / Math.Abs(previous) * HundredPercent);
    }

    public bool IsPositive => Value > 0m;
    public bool IsNegative => Value < 0m;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value:N1}%";
}
