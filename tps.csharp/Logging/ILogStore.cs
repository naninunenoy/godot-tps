using Microsoft.Extensions.Logging;

namespace tps.csharp;

public interface ILogStore
{
    IReadOnlyList<GameLogEntry> Entries { get; }
    IReadOnlyList<GameLogEntry> Errors { get; }
    bool HasEvent(string eventType,
        Func<IReadOnlyDictionary<string, object?>, bool>? predicate = null);
    void Add(GameLogEntry entry);
    void Clear();
}
