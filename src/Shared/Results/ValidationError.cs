namespace WeeklyUp.Shared.Results;

public sealed record ValidationError : AppError
{
    public IReadOnlyList<AppError> Errors { get; }

    public ValidationError(IReadOnlyList<AppError> errors)
        : base("Validation.General", "Erros de validacao encontrados.", AppErrorType.Validation)
    {
        Errors = errors;
    }
}
