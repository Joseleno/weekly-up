using System.Diagnostics;

using Mediator;

using Microsoft.Extensions.Logging;

namespace WeeklyUp.Application.Common.Behaviors;

public sealed class LoggingBehavior<TMessage, TResponse>
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private const int SlowRequestThresholdMs = 500;

    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger) =>
        _logger = logger;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        CancellationToken cancellationToken,
        MessageHandlerDelegate<TMessage, TResponse> next)
    {
        var messageName = typeof(TMessage).Name;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Handling {MessageName}", messageName);
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await next(message, cancellationToken);
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > SlowRequestThresholdMs)
        {
            _logger.LogWarning("Slow request detected: {MessageName} took {ElapsedMs}ms", messageName, elapsedMs);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Handled {MessageName} in {ElapsedMs}ms", messageName, elapsedMs);
        }

        return response;
    }
}
