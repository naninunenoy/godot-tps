using Microsoft.Extensions.Logging;
using tps.contract;
using VitalRouter;

namespace tps.csharp;

[Routes]
public sealed partial class KillSystem : IDisposable
{
    private readonly Router _router;
    private readonly ILogStore? _logStore;
    private int _killCount;
    private readonly IDisposable? _subscription;

    public int KillCount => _killCount;

    public KillSystem(Router router, ILogStore? logStore = null)
    {
        _router = router;
        _logStore = logStore;
        _subscription = this.MapTo(router);
    }

    [Route]
    public async ValueTask On(TargetDestroyedCommand _)
    {
        _killCount++;
        _logStore?.Add(new GameLogEntry(
            LogLevel.Information,
            GameEvents.KillCountChanged,
            new Dictionary<string, object?> { ["Count"] = _killCount },
            DateTimeOffset.UtcNow));
        await _router.PublishAsync(new KillCountChangedCommand { Count = _killCount });
    }

    public void Dispose() => _subscription?.Dispose();
}
