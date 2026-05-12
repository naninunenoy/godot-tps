using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace tps.Logging;

public sealed class JsonlLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minLevel;
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public JsonlLoggerProvider(LogLevel minLevel, string filePath)
    {
        _minLevel = minLevel;
        var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    public ILogger CreateLogger(string categoryName) =>
        new JsonlLogger(categoryName, _minLevel, _writer, _lock);

    public void Dispose()
    {
        _writer.Flush();
        _writer.Dispose();
    }
}

internal sealed class JsonlLogger(string category, LogLevel minLevel, StreamWriter writer, object @lock) : ILogger
{
    public bool IsEnabled(LogLevel level) => level >= minLevel;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(level)) return;
        var entry = new
        {
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            lvl = level.ToString(),
            cat = category,
            msg = formatter(state, ex),
        };
        var json = JsonSerializer.Serialize(entry);
        lock (@lock)
            writer.WriteLine(json);
    }
}
