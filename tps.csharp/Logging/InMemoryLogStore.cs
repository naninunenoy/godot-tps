using Microsoft.Extensions.Logging;

namespace tps.csharp;

public sealed class InMemoryLogStore : ILogStore
{
    private readonly List<GameLogEntry> _entries = [];

    public IReadOnlyList<GameLogEntry> Entries => _entries;

    public IReadOnlyList<GameLogEntry> Errors =>
        _entries.Where(e => e.Level >= LogLevel.Error).ToList();

    public bool HasEvent(
        string eventType,
        Func<IReadOnlyDictionary<string, object?>, bool>? predicate = null
    ) =>
        _entries.Any(e =>
            e.EventType == eventType && (predicate == null || predicate(e.Properties))
        );

    public void Add(GameLogEntry entry) => _entries.Add(entry);

    public void Clear() => _entries.Clear();
}
