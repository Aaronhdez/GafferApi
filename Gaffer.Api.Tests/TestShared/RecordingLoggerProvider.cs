using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Gaffer.Api.Tests.TestShared;

public record RecordedLogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

/// <summary>In-memory logger provider used to assert log entries without extra packages.</summary>
public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<RecordedLogEntry> entries = new();

    public IReadOnlyList<RecordedLogEntry> Entries => entries.ToList();

    public IReadOnlyList<RecordedLogEntry> EntriesFor<TCategory>() =>
        entries.Where(entry => entry.Category == typeof(TCategory).FullName).ToList();

    public ILogger<TCategory> CreateLogger<TCategory>() => new Logger<TCategory>(new LoggerFactory([this]));

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, entries);

    public void Dispose()
    {
    }

    private sealed class RecordingLogger(string category, ConcurrentQueue<RecordedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            entries.Enqueue(new RecordedLogEntry(category, logLevel, formatter(state, exception), exception));
        }
    }
}
