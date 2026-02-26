namespace WeeklyUp.Domain.Exceptions;

#pragma warning disable CA1032 // Construtores padrao omitidos intencionalmente: Code e obrigatorio por design de dominio
public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
#pragma warning restore CA1032
