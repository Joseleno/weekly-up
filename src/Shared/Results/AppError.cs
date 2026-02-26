namespace WeeklyUp.Shared.Results;

public record AppError(string Code, string Message, AppErrorType Type)
{
    public static readonly AppError None = new(string.Empty, string.Empty, AppErrorType.None);
    public static readonly AppError NullValue = new("Error.NullValue", "Valor nulo fornecido.", AppErrorType.Failure);

    public static AppError NotFound(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(code, message, AppErrorType.NotFound);
    }

    public static AppError Validation(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(code, message, AppErrorType.Validation);
    }

    public static AppError Conflict(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(code, message, AppErrorType.Conflict);
    }

    public static AppError Failure(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(code, message, AppErrorType.Failure);
    }

    public static AppError Forbidden(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(code, message, AppErrorType.Forbidden);
    }
}
