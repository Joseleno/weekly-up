using Mediator;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Auth.Commands.VerifyEmailByToken;

public sealed class VerifyEmailByTokenCommandHandler
    : ICommandHandler<VerifyEmailByTokenCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public VerifyEmailByTokenCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async ValueTask<Result<bool>> Handle(
        VerifyEmailByTokenCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByVerificationTokenAsync(command.Token, cancellationToken);
        if (user is null)
        {
            return AppError.NotFound("Auth.InvalidToken", "Token de verificacao invalido.");
        }

        Result verifyResult = user.VerifyEmail(command.Token);
        if (verifyResult.IsFailure)
        {
            return verifyResult.Error;
        }

        _uow.Users.Update(user);
        return true;
    }
}
