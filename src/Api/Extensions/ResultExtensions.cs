using WeeklyUp.Shared.Results;

namespace WeeklyUp.Api.Extensions;

internal static class ResultExtensions
{
    internal static IResult ToProblem(this AppError error) => error.Type switch
    {
        AppErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
        AppErrorType.Validation => Results.UnprocessableEntity(new { error.Code, error.Message }),
        AppErrorType.Conflict => Results.Conflict(new { error.Code, error.Message }),
        AppErrorType.Forbidden => Results.Forbid(),
        _ => Results.Problem(error.Message),
    };
}
