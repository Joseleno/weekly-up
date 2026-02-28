using Mediator;

using Microsoft.Extensions.Logging;

using WeeklyUp.Application.Common.Notifications;
using WeeklyUp.Domain.Events;

namespace WeeklyUp.Application.Users.Events;

public sealed partial class UserEmailVerifiedNotificationHandler : INotificationHandler<DomainEventNotification>
{
    private readonly ILogger<UserEmailVerifiedNotificationHandler> _logger;

    public UserEmailVerifiedNotificationHandler(ILogger<UserEmailVerifiedNotificationHandler> logger)
        => _logger = logger;

    public ValueTask Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Event is not UserEmailVerifiedEvent evt)
        {
            return ValueTask.CompletedTask;
        }

        LogEmailVerified(evt.UserId);
        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email verificado para usuario {UserId}")]
    private partial void LogEmailVerified(Guid userId);
}
