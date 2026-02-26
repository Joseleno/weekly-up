using WeeklyUp.Domain.Common;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class BusinessName : ValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 100;

    public string Value { get; }

    private BusinessName(string value) => Value = value;

    public static Result<BusinessName> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return AppError.Validation("BusinessName.Empty", "Nome do negocio nao pode ser vazio.");
        }

        name = name.Trim();

        if (name.Length < MinLength)
        {
            return AppError.Validation("BusinessName.TooShort", $"Nome do negocio deve ter pelo menos {MinLength} caracteres.");
        }

        if (name.Length > MaxLength)
        {
            return AppError.Validation("BusinessName.TooLong", $"Nome do negocio nao pode ter mais de {MaxLength} caracteres.");
        }

        return new BusinessName(name);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
