using Microsoft.Extensions.Logging;

namespace tps.csharp;

public record GameLogEntry(
    LogLevel Level,
    string EventType,
    IReadOnlyDictionary<string, object?> Properties,
    DateTimeOffset Timestamp,
    ulong FrameCount = 0
);
