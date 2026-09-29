using Microsoft.Extensions.Logging;

namespace AmusementPark.WebAPI.Tests.TestDoubles;

public sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = new List<(LogLevel, EventId, string)>();

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        this.Entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}
