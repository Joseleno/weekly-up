using System.Runtime.CompilerServices;

namespace WeeklyUp.Shared.Guards;

public static class Guard
{
    public static string AgainstNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} nao pode ser nulo ou vazio.", paramName);
        }
        return value;
    }

    public static T AgainstNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string paramName = "") where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
        return value;
    }

    public static Guid AgainstEmpty(
        Guid value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{paramName} nao pode ser vazio.", paramName);
        }
        return value;
    }

    public static decimal AgainstNegative(
        decimal value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (value < 0)
        {
            throw new ArgumentException($"{paramName} nao pode ser negativo.", paramName);
        }
        return value;
    }

    public static int AgainstNegative(
        int value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (value < 0)
        {
            throw new ArgumentException($"{paramName} nao pode ser negativo.", paramName);
        }
        return value;
    }
}
