using System.IO;
using Godot;
using Microsoft.Extensions.Logging;

namespace tps.Logging;

public static class AppLogger
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(b =>
    {
        var level = OS.IsDebugBuild() ? LogLevel.Debug : LogLevel.Warning;
        b.SetMinimumLevel(level);
        b.AddProvider(new GodotLoggerProvider(level));
        if (OS.IsDebugBuild())
        {
            var logPath = Path.Combine(OS.GetUserDataDir(), "debug.jsonl");
            b.AddProvider(new JsonlLoggerProvider(LogLevel.Debug, logPath));
        }
    });

    public static ILogger<T> For<T>() => Factory.CreateLogger<T>();
}
