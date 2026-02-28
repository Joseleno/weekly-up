using Mediator;

using Microsoft.Extensions.Logging;

using WeeklyUp.Application.Common.Notifications;
using WeeklyUp.Domain.Events;
using WeeklyUp.Domain.Interfaces.Services;

namespace WeeklyUp.Application.Users.Events;

public sealed partial class UserRegisteredNotificationHandler : INotificationHandler<DomainEventNotification>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UserRegisteredNotificationHandler> _logger;

    public UserRegisteredNotificationHandler(
        IEmailSender emailSender,
        ILogger<UserRegisteredNotificationHandler> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async ValueTask Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Event is not UserRegisteredEvent evt)
        {
            return;
        }

        var welcomeResult = await _emailSender.SendWelcomeAsync(evt.Email, evt.Name, cancellationToken);
        if (welcomeResult.IsFailure)
        {
            LogWelcomeEmailFailed(evt.UserId, welcomeResult.Error.Code);
        }

        if (evt.VerificationToken is not null)
        {
            var verifyResult = await _emailSender.SendVerificationEmailAsync(
                evt.Email, evt.Name, evt.VerificationToken, cancellationToken);
            if (verifyResult.IsFailure)
            {
                LogVerificationEmailFailed(evt.UserId, verifyResult.Error.Code);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao enviar email de boas-vindas para usuario {UserId}: {ErrorCode}")]
    private partial void LogWelcomeEmailFailed(Guid userId, string errorCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao enviar email de verificacao para usuario {UserId}: {ErrorCode}")]
    private partial void LogVerificationEmailFailed(Guid userId, string errorCode);
}
