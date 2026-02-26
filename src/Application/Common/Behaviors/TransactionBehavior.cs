using Mediator;
using Microsoft.Extensions.Logging;
using WeeklyUp.Domain.Interfaces;

namespace WeeklyUp.Application.Common.Behaviors;

public sealed class TransactionBehavior<TMessage, TResponse>
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage, ICommand<TResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TransactionBehavior<TMessage, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork uow,
        ILogger<TransactionBehavior<TMessage, TResponse>> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken cancellationToken,
        MessageHandlerDelegate<TMessage, TResponse> next)
    {
        var messageName = typeof(TMessage).Name;

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Beginning transaction for {MessageName}", messageName);
            }

            var response = await next(message, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Committed transaction for {MessageName}", messageName);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {MessageName}", messageName);
            throw;
        }
    }
}
