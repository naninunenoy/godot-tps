using Godot;
using Microsoft.Extensions.Logging;

namespace tps.Logging;

public static class AppLogger
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(b =>
    {
        var level = OS.IsDebugBuild() ? LogLevel.Debug : LogLevel.Warning;
        b.AddProvider(new GodotLoggerProvider(level));
    });

    public static ILogger<T> For<T>() => Factory.CreateLogger<T>();
}
