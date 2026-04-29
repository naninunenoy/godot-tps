using System;
using Godot;
using Microsoft.Extensions.Logging;

namespace tps.Logging;

public sealed class GodotLoggerProvider(LogLevel minLevel) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new GodotLogger(categoryName, minLevel);
    public void Dispose() { }
}

internal sealed class GodotLogger(string category, LogLevel minLevel) : ILogger
{
    public bool IsEnabled(LogLevel level) => level >= minLevel;
    public IDisposable? BeginScope<T>(T state) => NullScope.Instance;

    public void Log<T>(LogLevel level, EventId id, T state, Exception? ex, Func<T, Exception?, string> formatter)
    {
        if (!IsEnabled(level)) return;
        var msg = $"[{level}][{category}] {formatter(state, ex)}";
        switch (level)
        {
            case >= LogLevel.Error:
                GD.PushError(msg);
                break;
            case LogLevel.Warning:
                GD.PushWarning(msg);
                break;
            default:
                GD.Print(msg);
                break;
        }
    }
}

internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();
    public void Dispose() { }
}
