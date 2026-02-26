namespace WeeklyUp.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public AppError Error { get; }

    protected Result(bool isSuccess, AppError error)
    {
        if (isSuccess && error != AppError.None)
            throw new InvalidOperationException("Resultado de sucesso nao pode ter erro.");
        if (!isSuccess && error == AppError.None)
            throw new InvalidOperationException("Resultado de falha deve ter erro.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, AppError.None);
    public static Result Failure(AppError error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) =>
        new(value, true, AppError.None);

    public static Result<TValue> Failure<TValue>(AppError error) =>
        new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Nao e possivel acessar o valor de um resultado falho.");

    internal Result(TValue? value, bool isSuccess, AppError error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<AppError, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    public static implicit operator Result<TValue>(TValue value) =>
        Success(value);

    public static implicit operator Result<TValue>(AppError error) =>
        Failure<TValue>(error);
}
