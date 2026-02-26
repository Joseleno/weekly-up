using System.Text.RegularExpressions;

using WeeklyUp.Domain.Common;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    private static readonly Regex _emailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private const int MaxLength = 255;

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return AppError.Validation("Email.Empty", "Email nao pode ser vazio.");
        }

        email = email.Trim().ToLowerInvariant();

        if (email.Length > MaxLength)
        {
            return AppError.Validation("Email.TooLong", $"Email nao pode ter mais de {MaxLength} caracteres.");
        }

        if (!_emailRegex.IsMatch(email))
        {
            return AppError.Validation("Email.InvalidFormat", "Email em formato invalido.");
        }

        return new Email(email);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
