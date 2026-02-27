using Microsoft.Extensions.Logging;

namespace WeeklyUp.Application.Tests.Infrastructure;

/// <summary>
/// Logger de teste que captura todas as chamadas de log,
/// compatível com [LoggerMessage] source generator que usa tipos internos de TState.
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, string Message)> _entries = [];

    public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

    public bool HasEntry(LogLevel level) => _entries.Any(e => e.Level == level);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add((logLevel, formatter(state, exception)));
    }
}
